import { ConfigProvider, App as AntApp, Layout, Menu, Typography, Avatar, Dropdown, Space, Button } from 'antd';
import { Link, NavLink, Outlet, useNavigate } from 'react-router-dom';
import { useMemo, useState } from 'react';
import {
  DashboardOutlined,
  KeyOutlined,
  FileExcelOutlined,
  LogoutOutlined,
  UserOutlined,
  MailOutlined,
} from '@ant-design/icons';
import { antdTheme, appName, brandOrange, brandOrangeDark, brandOrangeTint } from '../shared/theme';
import { useAuth } from './auth-context';

const { Header, Sider, Content } = Layout;

type NavEntry = { key: string; path: string; label: string; icon: JSX.Element; roles: readonly ('administrator' | 'teacher')[] };

const navEntries: readonly NavEntry[] = [
  { key: 'dashboard', path: '/dashboard', label: 'Tổng quan', icon: <DashboardOutlined />, roles: ['administrator', 'teacher'] },
  { key: 'announcements', path: '/announcements', label: 'Thông báo & Email', icon: <MailOutlined />, roles: ['administrator', 'teacher'] },
  { key: 'password-help-requests', path: '/password-help-requests', label: 'Yêu cầu hỗ trợ mật khẩu', icon: <KeyOutlined />, roles: ['administrator'] },
  { key: 'imports', path: '/imports', label: 'Nhập liệu Excel', icon: <FileExcelOutlined />, roles: ['administrator'] },
];

export function AuthenticatedShell(): JSX.Element {
  const { state, logout } = useAuth();
  const navigate = useNavigate();
  const [collapsed, setCollapsed] = useState(false);

  const items = useMemo(() => {
    if (state.status !== 'authenticated' && state.status !== 'restricted') return [];
    const userRoles = state.status === 'restricted' ? state.user.roles : state.user.roles;
    return navEntries
      .filter((entry) => entry.roles.some((role) => userRoles.includes(role)))
      .map((entry) => ({
        key: entry.key,
        icon: entry.icon,
        label: <NavLink to={entry.path}>{entry.label}</NavLink>,
      }));
  }, [state]);

  const handleLogout = async (): Promise<void> => {
    await logout();
    navigate('/login', { replace: true });
  };

  const userMenu = {
    items: [
      {
        key: 'logout',
        label: 'Đăng xuất',
        icon: <LogoutOutlined />,
        onClick: handleLogout,
      },
    ],
  };

  return (
    <ConfigProvider theme={antdTheme}>
      <AntApp>
        <Layout style={{ minHeight: '100vh' }}>
          <Sider
            collapsible
            collapsed={collapsed}
            onCollapse={setCollapsed}
            width={260}
            style={{ borderRight: '1px solid #E0E0E0' }}
          >
            <div
              style={{
                padding: collapsed ? 16 : 20,
                display: 'flex',
                alignItems: 'center',
                gap: 12,
                borderBottom: '1px solid #E0E0E0',
              }}
            >
              <div
                style={{
                  width: 36,
                  height: 36,
                  borderRadius: 8,
                  background: brandOrange,
                  color: '#fff',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  fontWeight: 700,
                  fontSize: 16,
                  flexShrink: 0,
                }}
                aria-hidden
              >
                F
              </div>
              {!collapsed && (
                <div>
                  <Typography.Text strong style={{ color: brandOrangeDark, fontSize: 16 }}>
                    {appName}
                  </Typography.Text>
                  <div style={{ color: '#666', fontSize: 12 }}>Cổng quản trị</div>
                </div>
              )}
            </div>
            <Menu
              mode="inline"
              selectedKeys={[]}
              items={items}
              style={{ borderInlineEnd: 'none', padding: 12 }}
            />
            <div style={{ position: 'absolute', bottom: 16, left: 16, right: 16 }}>
              <Button
                block
                type="default"
                icon={<LogoutOutlined />}
                onClick={handleLogout}
                style={{ borderColor: '#E0E0E0' }}
              >
                {!collapsed && 'Đăng xuất'}
              </Button>
            </div>
          </Sider>
          <Layout>
            <Header
              style={{
                padding: '0 24px',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'space-between',
                borderBottom: '1px solid #E0E0E0',
                background: '#FFFFFF',
              }}
            >
              <Typography.Title level={4} style={{ margin: 0, color: brandOrangeDark }}>
                {appName}
              </Typography.Title>
              {state.status === 'authenticated' || state.status === 'restricted' ? (
                <Dropdown menu={userMenu} placement="bottomRight">
                  <Space style={{ cursor: 'pointer' }}>
                    <Avatar style={{ background: brandOrangeTint, color: brandOrangeDark }}>
                      {state.user.displayName.charAt(0).toUpperCase()}
                    </Avatar>
                    <span>
                      <Typography.Text strong>{state.user.displayName}</Typography.Text>
                      <br />
                      <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                        <UserOutlined /> {state.user.roles.map((r) => r).join(', ')}
                      </Typography.Text>
                    </span>
                  </Space>
                </Dropdown>
              ) : null}
            </Header>
            <Content style={{ padding: 24, background: brandOrangeTint }}>
              <div style={{ background: '#FFFFFF', padding: 24, borderRadius: 12, minHeight: '100%' }}>
                <Outlet />
              </div>
            </Content>
          </Layout>
        </Layout>
      </AntApp>
    </ConfigProvider>
  );
}

export const shellLink = (to: string, label: string): JSX.Element => (
  <Link to={to}>{label}</Link>
);
