import {
  BarChart3,
  BookOpen,
  Boxes,
  Bus,
  CalendarClock,
  ClipboardList,
  FileText,
  GraduationCap,
  LayoutDashboard,
  Megaphone,
  Settings,
  Users,
  UserRound,
  WalletCards,
} from "lucide-react";

export const adminNavigation = [
  {
    label: "MAIN",
    items: [
      {
        title: "Dashboard",
        href: "dashboard",
        icon: LayoutDashboard,
      },
      {
        title: "Admissions",
        href: "admissions",
        icon: UserRound,
      },
      {
        title: "Students",
        href: "students",
        icon: Users,
      },
      {
        title: "Staff",
        href: "staff",
        icon: GraduationCap,
      },
    ],
  },
  {
    label: "ACADEMICS",
    items: [
      {
        title: "Academics",
        href: "academics",
        icon: BookOpen,
      },
      {
        title: "Timetable",
        href: "timetable",
        icon: CalendarClock,
      },
      {
        title: "Assessments",
        href: "assessments",
        icon: ClipboardList,
      },
    ],
  },
  {
    label: "FINANCE",
    items: [
      {
        title: "Finance",
        href: "finance",
        icon: WalletCards,
      },
      {
        title: "Fees Management",
        href: "fees-management",
        icon: FileText,
      },
    ],
  },
  {
    label: "OPERATIONS",
    items: [
      {
        title: "Inventory",
        href: "inventory",
        icon: Boxes,
      },
      {
        title: "Communication",
        href: "communication",
        icon: Megaphone,
      },
      {
        title: "Transport",
        href: "transport",
        icon: Bus,
      },
    ],
  },
  {
    label: "REPORTS",
    items: [
      {
        title: "Reports",
        href: "reports",
        icon: BarChart3,
      },
    ],
  },
  {
    label: "SETTINGS",
    items: [
      {
        title: "Settings",
        href: "settings",
        icon: Settings,
      },
    ],
  },
];
