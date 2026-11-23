import type { ReactNode } from "react";

/** Decorative stroke icons; every button that uses one carries its own accessible name. */
function Icon({ children, className = "jp-icon" }: { children: ReactNode; className?: string }) {
  return <svg className={className} viewBox="0 0 24 24" aria-hidden="true" focusable="false">{children}</svg>;
}

export const PinIcon = ({ filled }: { filled: boolean }) =>
  <Icon><path className={filled ? "jp-icon-fill" : undefined} d="M9 3h6l-1 6 3 3v2H7v-2l3-3z" /><path d="M12 14v7" /></Icon>;
export const SettingsIcon = () =>
  <Icon><path d="M4 7h9" /><path d="M17 7h3" /><circle cx="15" cy="7" r="2" /><path d="M4 17h3" /><path d="M11 17h9" /><circle cx="9" cy="17" r="2" /></Icon>;
export const CloseIcon = () => <Icon><path d="M6 6l12 12" /><path d="M18 6L6 18" /></Icon>;
export const SpeakerIcon = () =>
  <Icon><path d="M4 9.5v5h3.5L12 18V6L7.5 9.5z" /><path d="M15.5 9a4 4 0 0 1 0 6" /><path d="M18 6.5a7.5 7.5 0 0 1 0 11" /></Icon>;
export const WarningIcon = () => <Icon><path d="M12 4 2.8 20h18.4z" /><path d="M12 10v4.5" /><path d="M12 17.4v.1" /></Icon>;
export const PointerIcon = () => <Icon><path d="M6 3l12 9-5.5 1.2L10 19z" /></Icon>;
export const StopIcon = () => <Icon><rect x="7" y="7" width="10" height="10" rx="1.5" /></Icon>;
export const ReplayIcon = () => <Icon><path d="M4.5 12a7.5 7.5 0 1 0 2.2-5.3" /><path d="M4.5 4.5v4h4" /></Icon>;
export const GripIcon = () => <Icon className="jp-icon jp-grip"><path d="M20 13 13 20" /><path d="M20 7 7 20" /></Icon>;
