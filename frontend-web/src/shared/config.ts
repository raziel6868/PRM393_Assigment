export const apiBaseUrl: string = __API_BASE_URL__;

export type SchoolRole = 'administrator' | 'teacher' | 'parent' | 'student';

export const allRoles: readonly SchoolRole[] = ['administrator', 'teacher', 'parent', 'student'];

export function isPortalActor(roles: readonly SchoolRole[]): boolean {
  return roles.includes('administrator') || roles.includes('teacher');
}

export const roleDisplay: Record<SchoolRole, string> = {
  administrator: 'Quản trị viên',
  teacher: 'Giáo viên',
  parent: 'Phụ huynh',
  student: 'Học sinh',
};
