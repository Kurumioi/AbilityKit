const adminPrefix = 'abilitykit.admin.';
const legacyPrefix = 'abilitykit.';

export const adminStorage = {
  get(key: string, fallback = ''): string {
    return localStorage.getItem(`${adminPrefix}${key}`) || localStorage.getItem(`${legacyPrefix}${key}`) || fallback;
  },
  set(key: string, value: string | null | undefined): void {
    localStorage.setItem(`${adminPrefix}${key}`, value || '');
  }
};
