type Paper = "a4-landscape" | "a3-landscape";

function escapePdfText(value: string) {
  return value.replace(/\\/g, "\\\\").replace(/\(/g, "\\(").replace(/\)/g, "\\)").replace(/[^\x20-\x7e]/g, "?");
}

export function downloadTimetablePdf(filename: string, title: string, lines: string[], paper: Paper = "a4-landscape", schoolName = "School timetable") {
  const pdf = buildTimetablePdf(title, lines, paper, schoolName);
  const blob = new Blob([pdf], { type: "application/pdf" });
  const link = document.createElement("a");
  link.href = URL.createObjectURL(blob);
  link.download = filename;
  link.click();
  URL.revokeObjectURL(link.href);
}

export function buildTimetablePdf(title: string, lines: string[], paper: Paper = "a4-landscape", schoolName = "School timetable") {
  const page = paper === "a3-landscape" ? [1190, 842] : [842, 595];
  const linesPerPage = paper === "a3-landscape" ? 48 : 34;
  const pages: string[][] = [];
  for (let index = 0; index < lines.length || index === 0; index += linesPerPage) pages.push(lines.slice(index, index + linesPerPage));
  const objects: string[] = ["<< /Type /Catalog /Pages 2 0 R >>"];
  const pageObjectIds = pages.map((_, index) => 4 + index * 2);
  objects.push(`<< /Type /Pages /Kids [${pageObjectIds.map(id => `${id} 0 R`).join(" ")}] /Count ${pageObjectIds.length} >>`);
  objects.push("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");
  pages.forEach((pageLines, pageIndex) => {
    const pageId = 4 + pageIndex * 2;
    const contentId = pageId + 1;
    const streamLines = [`BT /F1 11 Tf 36 ${page[1] - 42} Td (${escapePdfText(title)}) Tj`, `0 -16 Td (${escapePdfText(schoolName)}) Tj`];
    pageLines.forEach(line => streamLines.push(`0 -14 Td (${escapePdfText(line.slice(0, 150))}) Tj`));
    streamLines.push("ET");
    const stream = streamLines.join("\n");
    objects.push(`<< /Type /Page /Parent 2 0 R /MediaBox [0 0 ${page[0]} ${page[1]}] /Resources << /Font << /F1 3 0 R >> >> /Contents ${contentId} 0 R >>`);
    objects.push(`<< /Length ${stream.length} >>\nstream\n${stream}\nendstream`);
  });
  let pdf = "%PDF-1.4\n";
  const offsets = [0];
  objects.forEach((object, index) => { offsets[index + 1] = pdf.length; pdf += `${index + 1} 0 obj\n${object}\nendobj\n`; });
  const xref = pdf.length;
  pdf += `xref\n0 ${objects.length + 1}\n0000000000 65535 f \n${offsets.slice(1).map(offset => `${String(offset).padStart(10, "0")} 00000 n `).join("\n")}\ntrailer\n<< /Size ${objects.length + 1} /Root 1 0 R >>\nstartxref\n${xref}\n%%EOF`;
  return pdf;
}

export function printTimetable(title: string, lines: string[]) {
  const popup = window.open("", "_blank", "noopener,noreferrer,width=1100,height=800");
  if (!popup) return;
  popup.document.write(`<html><head><title>${title}</title><style>@page{size:landscape;margin:12mm}body{font:12px Arial;color:#111}h1{font-size:20px}pre{white-space:pre-wrap;font:11px Arial;line-height:1.5}</style></head><body><h1>${title}</h1><pre>${lines.join("\n").replace(/</g, "&lt;")}</pre></body></html>`);
  popup.document.close();
  popup.focus();
  popup.print();
}
