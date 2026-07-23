import type { ThemeConfig } from 'antd';

export const brandOrange = '#E65100';
export const brandOrangeDark = '#BF360C';
export const brandOrangeTint = '#FFF3E0';
export const surfaceBase = '#FAFAFA';
export const surfaceRaised = '#FFFFFF';
export const textPrimary = '#1A1A1A';
export const textSecondary = '#666666';
export const borderColor = '#E0E0E0';

export const statusColor = {
  present: '#2E7D32',
  pending: '#F57C00',
  rejected: '#C62828',
} as const;

export const antdTheme: ThemeConfig = {
  token: {
    colorPrimary: brandOrange,
    colorLink: brandOrange,
    colorLinkHover: brandOrangeDark,
    colorBgBase: surfaceBase,
    colorText: textPrimary,
    colorTextSecondary: textSecondary,
    colorBorder: borderColor,
    borderRadius: 8,
    fontFamily:
      '"Inter", "Segoe UI", -apple-system, BlinkMacSystemFont, "Helvetica Neue", Arial, sans-serif',
  },
  components: {
    Layout: {
      headerBg: surfaceRaised,
      siderBg: surfaceRaised,
      bodyBg: surfaceBase,
      headerHeight: 64,
    },
    Menu: {
      itemSelectedBg: brandOrangeTint,
      itemSelectedColor: brandOrange,
      itemHoverBg: brandOrangeTint,
      itemHoverColor: brandOrangeDark,
      itemBorderRadius: 8,
    },
    Button: {
      colorPrimaryHover: brandOrangeDark,
      colorPrimaryActive: brandOrangeDark,
    },
  },
};

export const appName = 'MyFSchool';
