/**
 * Produce a small, standards-compliant Excel workbook without an external XLSX
 * dependency. The workbook is an administrative export of the existing backend
 * gradebook; it does NOT represent an approved or published student report.
 * Scores/weights are copied from the authorized backend response, not recalculated.
 */
import { Buffer } from "node:buffer";

type Column = {
  assessmentId: string;
  title: string;
  maximumScore: number;
  weightPercentage: number;
};

type StudentScore = {
  assessmentId: string;
  rawScore: number | null;
  weightedContribution: number | null;
};

type StudentRow = {
  admissionNumber: string;
  studentName: string;
  totalWeightedScore: number;
  scores: StudentScore[];
};

export type GradebookForExport = {
  academicTermId: string;
  classGroupName: string;
  subjectName: string;
  configuredWeightTotal: number;
  assessments: Column[];
  students: StudentRow[];
};

function escapeXml(input: string): string {
  return input
    .replace(/[\u0000-\u0008\u000B\u000C\u000E-\u001F\uFFFE\uFFFF]/g, "")
    .slice(0, 32767)
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;")
    .replace(/'/g, "&apos;");
}

function columnRef(index: number): string {
  let result = "";
  for (let n = index; n > 0; n = Math.floor((n - 1) / 26)) {
    result = String.fromCharCode(65 + ((n - 1) % 26)) + result;
  }
  return result;
}

function cell(row: number, col: number, value: string | number | null, style = 0): string {
  const address = `${columnRef(col)}${row}`;
  const styleAttr = style ? ` s="${style}"` : "";
  if (typeof value === "number") {
    if (Number.isFinite(value)) {
      return `<c r="${address}"${styleAttr}><v>${value}</v></c>`;
    }
    return `<c r="${address}"${styleAttr}/>`;
  }
  if (value === null) return `<c r="${address}"${styleAttr}/>`;
  // inlineStr (never formulas) prevents spreadsheet formula injection in names/titles.
  return `<c r="${address}" t="inlineStr"${styleAttr}><is><t xml:space="preserve">${escapeXml(value)}</t></is></c>`;
}

function sheetRow(row: number, values: Array<string | number | null>, style = 0): string {
  return `<row r="${row}">${values.map((v, index) => cell(row, index + 1, v, style)).join("")}</row>`;
}

function crc32(data: Buffer): number {
  let crc = 0xffffffff;
  for (const byte of data) {
    crc ^= byte;
    for (let bit = 0; bit < 8; bit++) {
      crc = (crc >>> 1) ^ (crc & 1 ? 0xedb88320 : 0);
    }
  }
  return (crc ^ 0xffffffff) >>> 0;
}

/** Minimal ZIP32 stored-file writer; XLSX requires a ZIP of Office XML parts. */
function zipStore(files: Array<{ name: string; content: string }>): Buffer {
  const parts: Buffer[] = [];
  const directory: Buffer[] = [];
  let offset = 0;
  for (const file of files) {
    const path = Buffer.from(file.name, "utf8");
    const data = Buffer.from(file.content, "utf8");
    const checksum = crc32(data);
    const local = Buffer.alloc(30);
    local.writeUInt32LE(0x04034b50, 0);
    local.writeUInt16LE(20, 4);
    local.writeUInt16LE(0x0800, 6); // UTF-8 names
    local.writeUInt32LE(checksum, 14);
    local.writeUInt32LE(data.length, 18);
    local.writeUInt32LE(data.length, 22);
    local.writeUInt16LE(path.length, 26);
    parts.push(local, path, data);

    const central = Buffer.alloc(46);
    central.writeUInt32LE(0x02014b50, 0);
    central.writeUInt16LE(20, 4);
    central.writeUInt16LE(20, 6);
    central.writeUInt16LE(0x0800, 8);
    central.writeUInt32LE(checksum, 16);
    central.writeUInt32LE(data.length, 20);
    central.writeUInt32LE(data.length, 24);
    central.writeUInt16LE(path.length, 28);
    central.writeUInt32LE(offset, 42);
    directory.push(central, path);
    offset += local.length + path.length + data.length;
  }
  const centralSize = directory.reduce((sum, part) => sum + part.length, 0);
  const footer = Buffer.alloc(22);
  footer.writeUInt32LE(0x06054b50, 0);
  footer.writeUInt16LE(files.length, 8);
  footer.writeUInt16LE(files.length, 10);
  footer.writeUInt32LE(centralSize, 12);
  footer.writeUInt32LE(offset, 16);
  return Buffer.concat([...parts, ...directory, footer]);
}

export function buildSubjectGradebookXlsx(gradebook: GradebookForExport): Buffer {
  const count = gradebook.assessments.length;
  if (count > 250 || gradebook.students.length > 20000) {
    throw new Error("The gradebook exceeds Excel export limits.");
  }
  const totalColumns = 2 + count * 2 + 3;
  const lastColumn = columnRef(totalColumns);
  const weightConfigured = Number(gradebook.configuredWeightTotal) === 100;
  const headers: Array<string | number | null> = ["Student", "Admission number"];
  for (const assessment of gradebook.assessments) {
    headers.push(`${assessment.title} / ${assessment.maximumScore} (raw)`);
    headers.push(`${assessment.title} (${assessment.weightPercentage}% contribution)`);
  }
  headers.push("Provisional total", "Scores entered", "Ready for review?");
  let completeStudents = 0;
  const rows: string[] = [
    sheetRow(1, [`Subject gradebook — ${gradebook.subjectName}`], 2),
    sheetRow(2, [`Class: ${gradebook.classGroupName}`, `Academic term ID: ${gradebook.academicTermId}`]),
    sheetRow(3, [`Configured weight: ${gradebook.configuredWeightTotal}%`, weightConfigured ? "Weights total 100%" : "INCOMPLETE WEIGHTS — NOT FINAL"], 3),
    sheetRow(4, ["Draft export only. Missing scores are blank, not zero. No result in this workbook is published to guardians."]),
    sheetRow(6, headers, 1),
  ];
  for (let index = 0; index < gradebook.students.length; index++) {
    const student = gradebook.students[index];
    const byAssessment = new Map(student.scores.map(s => [s.assessmentId, s]));
    const values: Array<string | number | null> = [student.studentName, student.admissionNumber];
    let entered = 0;
    for (const assessment of gradebook.assessments) {
      const score = byAssessment.get(assessment.assessmentId);
      const raw = score?.rawScore;
      const weighted = score?.weightedContribution;
      values.push(raw == null ? null : Number(raw));
      values.push(weighted == null ? null : Number(weighted));
      if (raw != null) entered++;
    }
    const ready = weightConfigured && count > 0 && entered === count;
    if (ready) completeStudents++;
    values.push(Number(student.totalWeightedScore));
    values.push(`${entered}/${count}`);
    values.push(ready ? "All scores entered" : "Incomplete — do not publish");
    rows.push(sheetRow(7 + index, values));
  }
  rows.push(sheetRow(8 + gradebook.students.length, [`Complete student score sets: ${completeStudents}/${gradebook.students.length}`]));
  const columnWidths = `<cols>${Array.from({ length: totalColumns }, (_, i) =>
    `<col min="${i + 1}" max="${i + 1}" width="${i === 0 ? 30 : i === 1 ? 20 : i >= totalColumns - 3 ? 24 : 19}" customWidth="1"/>`).join("")}</cols>`;
  const worksheet = `<?xml version="1.0" encoding="UTF-8" standalone="yes"?>` +
    `<worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">` +
    `<sheetViews><sheetView workbookViewId="0"><pane ySplit="6" topLeftCell="A7" activePane="bottomLeft" state="frozen"/></sheetView></sheetViews>` +
    columnWidths + `<sheetData>${rows.join("")}</sheetData>` +
    `<mergeCells count="2"><mergeCell ref="A1:${lastColumn}1"/><mergeCell ref="A4:${lastColumn}4"/></mergeCells></worksheet>`;
  const styles = `<?xml version="1.0" encoding="UTF-8" standalone="yes"?>` +
    `<styleSheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">` +
    `<fonts count="3"><font><sz val="11"/><name val="Aptos"/></font><font><b/><color rgb="FFFFFFFF"/><sz val="11"/></font><font><b/><sz val="15"/></font></fonts>` +
    `<fills count="4"><fill><patternFill patternType="none"/></fill><fill><patternFill patternType="gray125"/></fill><fill><patternFill patternType="solid"><fgColor rgb="FF223D5A"/><bgColor indexed="64"/></patternFill></fill><fill><patternFill patternType="solid"><fgColor rgb="FFFFF2D6"/><bgColor indexed="64"/></patternFill></fill></fills>` +
    `<borders count="1"><border><left/><right/><top/><bottom/><diagonal/></border></borders>` +
    `<cellStyleXfs count="1"><xf numFmtId="0" fontId="0" fillId="0" borderId="0"/></cellStyleXfs>` +
    `<cellXfs count="4"><xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0"/><xf numFmtId="0" fontId="1" fillId="2" borderId="0" xfId="0" applyFont="1" applyFill="1"/><xf numFmtId="0" fontId="2" fillId="0" borderId="0" xfId="0" applyFont="1"/><xf numFmtId="0" fontId="0" fillId="3" borderId="0" xfId="0" applyFill="1"/></cellXfs></styleSheet>`;
  return zipStore([
    { name: "[Content_Types].xml", content: `<?xml version="1.0" encoding="UTF-8"?><Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/><Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/><Override PartName="/xl/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"/></Types>` },
    { name: "_rels/.rels", content: `<?xml version="1.0" encoding="UTF-8"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/></Relationships>` },
    { name: "xl/workbook.xml", content: `<?xml version="1.0" encoding="UTF-8"?><workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><sheets><sheet name="Gradebook" sheetId="1" r:id="rId1"/></sheets></workbook>` },
    { name: "xl/_rels/workbook.xml.rels", content: `<?xml version="1.0" encoding="UTF-8"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/><Relationship Id="rId2" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/></Relationships>` },
    { name: "xl/styles.xml", content: styles },
    { name: "xl/worksheets/sheet1.xml", content: worksheet },
  ]);
}
