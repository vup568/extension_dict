/** RFC 4122 UUID v4 with injected entropy; no browser-vendor API in application logic. */
export function createInteractionId(fill: (bytes: Uint8Array<ArrayBuffer>) => void): string {
  const bytes = new Uint8Array(16);
  fill(bytes);
  bytes[6] = ((bytes[6] ?? 0) & 0x0f) | 0x40;
  bytes[8] = ((bytes[8] ?? 0) & 0x3f) | 0x80;
  const hex = Array.from(bytes, (byte) => byte.toString(16).padStart(2, "0")).join("");
  return `${hex.slice(0, 8)}-${hex.slice(8, 12)}-${hex.slice(12, 16)}-${hex.slice(16, 20)}-${hex.slice(20)}`;
}
