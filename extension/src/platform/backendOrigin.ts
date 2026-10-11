/** EARS[Ubiquitous]: Backend communication SHALL be encrypted outside loopback development. */
export function validateBackendOrigin(value: string): string {
  let url: URL;
  try { url = new URL(value); } catch { throw new Error("Invalid backend origin configuration."); }
  const loopback = ["localhost", "127.0.0.1", "[::1]"].includes(url.hostname);
  if (url.username || url.password || url.pathname !== "/" || url.search || url.hash ||
    !(url.protocol === "https:" || (url.protocol === "http:" && loopback))) {
    throw new Error("Backend configuration requires an HTTPS origin or local loopback HTTP origin.");
  }
  return url.origin;
}
