import {
  Alert,
  Button,
  Card,
  Empty,
  List,
  Space,
  Tag,
  Typography,
} from 'antd';
import { PlusOutlined, SendOutlined } from '@ant-design/icons';
import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { announcementsApi } from '../../api/announcements';
import { isApiError } from '../../api/errors';
import { formatSchoolDateTime } from '../../shared/format';

const audienceLabel: Record<string, string> = {
  SchoolWide: 'Toàn trường',
  Class: 'Theo lớp',
  Teacher: 'Giáo viên',
  Parent: 'Phụ huynh',
  Student: 'Học sinh',
};

export function AnnouncementsPage(): JSX.Element {
  const query = useQuery({
    queryKey: ['announcements', 1],
    queryFn: () => announcementsApi.list(),
  });

  return (
    <Space direction="vertical" size={20} style={{ width: '100%' }}>
      <div
        style={{
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'flex-start',
          gap: 16,
        }}
      >
        <div>
          <Typography.Title level={3} style={{ margin: 0 }}>
            Thông báo & Email
          </Typography.Title>
          <Typography.Paragraph type="secondary" style={{ marginBottom: 0 }}>
            Soạn và gửi thông báo tới đúng nhóm người nhận được phép.
          </Typography.Paragraph>
        </div>
        <Link to="/announcements/new">
          <Button type="primary" icon={<PlusOutlined />}>
            Soạn thông báo
          </Button>
        </Link>
      </div>

      {query.isError && (
        <Alert
          type="error"
          showIcon
          message={
            isApiError(query.error)
              ? query.error.userMessage
              : 'Không thể tải danh sách thông báo.'
          }
        />
      )}

      <Card loading={query.isLoading}>
        {query.data?.items.length === 0 ? (
          <Empty description="Chưa có thông báo đã phát hành." />
        ) : (
          <List
            dataSource={query.data?.items ?? []}
            renderItem={(item) => (
              <List.Item>
                <List.Item.Meta
                  avatar={<SendOutlined style={{ fontSize: 22, color: '#F15A24' }} />}
                  title={
                    <Space wrap>
                      <Typography.Text strong>{item.title}</Typography.Text>
                      <Tag color="orange">
                        {audienceLabel[item.audience] ?? item.audience}
                      </Tag>
                      {item.targetClassName && <Tag>{item.targetClassName}</Tag>}
                    </Space>
                  }
                  description={
                    <Space direction="vertical" size={4}>
                      <Typography.Text>{item.bodyPreview}</Typography.Text>
                      <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                        {item.authorDisplayName} ·{' '}
                        {formatSchoolDateTime(item.publishedAtUtc ?? item.createdAtUtc)} ·{' '}
                        {item.totalRecipientCount} lượt phân phối
                      </Typography.Text>
                    </Space>
                  }
                />
              </List.Item>
            )}
          />
        )}
      </Card>
    </Space>
  );
}
