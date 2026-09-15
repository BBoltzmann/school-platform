import { BookOpen, CalendarClock, GraduationCap, LayoutDashboard, UserRound, Users } from "lucide-react";

export const teacherNavigation = [
  { label: "MAIN", items: [{ title: "Dashboard", href: "dashboard", icon: LayoutDashboard }] },
  { label: "TEACHING", items: [
    { title: "My Classes", href: "teacher/classes", icon: GraduationCap },
    { title: "My Students", href: "teacher/students", icon: Users },
    { title: "My Subjects", href: "teacher/subjects", icon: BookOpen },
    { title: "My Timetable", href: "teacher/timetable", icon: CalendarClock },
  ] },
  { label: "ACCOUNT", items: [{ title: "My Profile", href: "teacher/profile", icon: UserRound }] },
];
