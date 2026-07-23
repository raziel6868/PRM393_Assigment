import { apiRequest } from './client';

export type AnnouncementAudience =
  | 'schoolWide'
  | 'class'
  | 'teacher'
  | 'parent'
  | 'student';

export type DeliveryChannel = 'portalApp' | 'email';

export type AnnouncementListItem = {
  id: string;
  title: string;
  bodyPreview: string;
  audience: string;
  targetClassName: string | null;
  authorDisplayName: string;
  createdAtUtc: string;
  publishedAtUtc: string | null;
  imageUrl: string | null;
  readCount: number;
  totalRecipientCount: number;
};

export type AnnouncementDetail = {
  id: string;
  title: string;
  body: string;
  audience: string;
  targetClassName: string | null;
  authorDisplayName: string;
  createdAtUtc: string;
  publishedAtUtc: string | null;
  imageUrl: string | null;
  readCount: number;
  totalRecipientCount: number;
  rowVersion: string;
};

export type AnnouncementPage = {
  items: AnnouncementListItem[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
};

export type TeacherClassScope = {
  classId: string;
  classCode: string;
  classDisplayName: string;
  subjectId: string;
  subjectCode: string;
  subjectDisplayName: string;
  schoolYearId: string;
  schoolYearCode: string;
  isHomeroom: boolean;
};

export type CreateAnnouncementInput = {
  title: string;
  body: string;
  audience: AnnouncementAudience;
  targetClassId: string | null;
  imageUrl: string | null;
};

export const announcementsApi = {
  list(page = 1, pageSize = 20) {
    return apiRequest<AnnouncementPage>(
      `/api/v1/announcements?page=${page}&pageSize=${pageSize}`,
    );
  },
  create(input: CreateAnnouncementInput) {
    return apiRequest<AnnouncementDetail>('/api/v1/announcements', {
      method: 'POST',
      body: input,
    });
  },
  publish(
    announcementId: string,
    rowVersion: string,
    deliveryChannels: DeliveryChannel[],
  ) {
    return apiRequest<void>(`/api/v1/announcements/${announcementId}/publish`, {
      method: 'POST',
      body: {
        rowVersion,
        deliveryChannels: deliveryChannels.map((channel) => ({ channel })),
      },
    });
  },
  myClasses() {
    return apiRequest<TeacherClassScope[]>('/api/v1/me/classes');
  },
};
