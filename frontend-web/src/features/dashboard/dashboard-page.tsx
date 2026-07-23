import { Button, Card, Empty, Space, Typography } from 'antd';
import { Link, useNavigate } from 'react-router-dom';
import { KeyOutlined, FileExcelOutlined, LogoutOutlined } from '@ant-design/icons';
import { useAuth } from '../../app/auth-context';
import { brandOrange, brandOrangeDark } from '../../shared/theme';

type ActionCard = {
  to: string;
  icon: JSX.Element;
  title: string;
  description: string;
  roles: readonly ('administrator' | 'teacher')[];
};

const actions: readonly ActionCard[] = [
  {
    to: '/password-help-requests',
    icon: <KeyOutlined />,
    title: 'Yêu cầu hỗ trợ mật khẩu',
    description: 'Xem và xử lý các yêu cầu hỗ trợ mật khẩu đang chờ.',
    roles: ['administrator'],
  },
  {
    to: '/imports',
    icon: <FileExcelOutlined />,
    title: 'Nhập liệu Excel',
    description: 'Tải lên sổ danh sách học sinh, phụ huynh và giáo viên.',
    roles: ['administrator'],
  },
];

export function DashboardPage(): JSX.Element {
  const { state, logout } = useAuth();
  const navigate = useNavigate();

  if (state.status !== 'authenticated') {
    return <Empty description="Đang tải..." />;
  }

  const user = state.user;
  const visibleActions = actions.filter((action) => action.roles.some((role) => user.roles.includes(role)));
  const isTeacherOnly = user.roles.includes('teacher') && !user.roles.includes('administrator');

  const handleLogout = async (): Promise<void> => {
    await logout();
    navigate('/login', { replace: true });
  };

  return (
    <Space direction="vertical" size={24} style={{ width: '100%' }}>
      <Card>
        <Typography.Title level={3} style={{ marginTop: 0, color: brandOrangeDark }}>
          Xin chào, {user.displayName}
        </Typography.Title>
        <Typography.Paragraph>
          {isTeacherOnly
            ? 'Bạn đang đăng nhập với vai trò giáo viên. Truy cập cổng quản trị bằng ứng dụng di động để thao tác chấm điểm danh, duyệt đơn nghỉ và các chức năng hằng ngày.'
            : 'Chào mừng bạn đến với cổng quản trị MyFSchool. Chọn thao tác bên dưới để tiếp tục.'}
        </Typography.Paragraph>
      </Card>

      {visibleActions.length === 0 ? (
        <Card>
          <Empty description="Không có thao tác nào khả dụng cho vai trò của bạn." />
        </Card>
      ) : (
        <div
          style={{
            display: 'grid',
            gap: 16,
            gridTemplateColumns: 'repeat(auto-fit, minmax(280px, 1fr))',
          }}
        >
          {visibleActions.map((action) => (
            <Card
              key={action.to}
              hoverable
              onClick={() => navigate(action.to)}
              style={{ cursor: 'pointer', borderColor: '#E0E0E0' }}
            >
              <Space align="start">
                <div
                  style={{
                    width: 48,
                    height: 48,
                    borderRadius: 12,
                    background: '#FFF3E0',
                    color: brandOrange,
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'center',
                    fontSize: 22,
                  }}
                  aria-hidden
                >
                  {action.icon}
                </div>
                <div>
                  <Typography.Title level={5} style={{ margin: 0, color: brandOrangeDark }}>
                    {action.title}
                  </Typography.Title>
                  <Typography.Paragraph type="secondary" style={{ marginBottom: 8 }}>
                    {action.description}
                  </Typography.Paragraph>
                  <Link to={action.to}>Mở →</Link>
                </div>
              </Space>
            </Card>
          ))}
        </div>
      )}

      <Card>
        <Button icon={<LogoutOutlined />} onClick={handleLogout}>
          Đăng xuất
        </Button>
      </Card>
    </Space>
  );
}
