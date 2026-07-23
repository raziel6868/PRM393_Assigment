import dayjs from 'dayjs';

export const schoolDateFormat = 'DD/MM/YYYY';
export const schoolDateTimeFormat = 'DD/MM/YYYY HH:mm';

export function formatSchoolDate(value: string | null | undefined): string {
  if (!value) return '—';
  const parsed = dayjs(value);
  return parsed.isValid() ? parsed.format(schoolDateFormat) : value;
}

export function formatSchoolDateTime(value: string | null | undefined): string {
  if (!value) return '—';
  const parsed = dayjs(value);
  return parsed.isValid() ? parsed.format(schoolDateTimeFormat) : value;
}
