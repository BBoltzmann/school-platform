import { publicAuthPost } from "@/lib/api/public-auth";

export async function POST(request: Request) {
  return publicAuthPost(request, "signup");
}
