import {
  Alert,
  Button,
  Card,
  Empty,
  Modal,
  Space,
  Table,
  Tag,
  Typography,
  message,
} from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { passwordHelpApi, type PasswordHelpItem } from '../../api/password-help';
import { isApiError } from '../../api/errors';
import { formatSchoolDateTime } from '../../shared/format';

const statusColor: Record<string, string> = {
  pending: 'orange',
  resolved: 'green',
  rejected: 'red',
};

const statusLabel: Record<string, string> = {
  pending: 'Đang chờ',
  resolved: 'Đã xử lý',
  rejected: 'Không xác minh được',
};

export function PasswordHelpRequestsPage(): JSX.Element {
  const queryClient = useQueryClient();
  const [messageApi, contextHolder] = message.useMessage();
  const [issuedPassword, setIssuedPassword] = useState<{ userId: string; password: string } | null>(null);
  const [rejectTarget, setRejectTarget] = useState<PasswordHelpItem | null>(null);

  const query = useQuery({
    queryKey: ['password-help', 'pending'],
    queryFn: () => passwordHelpApi.listPending(),
  });

  const rejectMutation = useMutation({
    mutationFn: ({ requestId, rowVersion }: { requestId: string; rowVersion: string }) =>
      passwordHelpApi.reject(requestId, rowVersion),
    onSuccess: () => {
      messageApi.success('Đã từ chối yêu cầu.');
      setRejectTarget(null);
      void queryClient.invalidateQueries({ queryKey: ['password-help'] });
    },
    onError: (err) => {
      messageApi.error(isApiError(err) ? err.userMessage : 'Không thể từ chối yêu cầu.');
    },
  });

  const issueMutation = useMutation({
    mutationFn: ({ userId, rowVersion }: { userId: string; rowVersion: string }) =>
      passwordHelpApi.issueTemporaryPassword(userId, rowVersion),
    onSuccess: (response) => {
      setIssuedPassword({ userId: response.userId, password: response.temporaryPassword });
      void queryClient.invalidateQueries({ queryKey: ['password-help'] });
    },
    onError: (err) => {
      messageApi.error(isApiError(err) ? err.userMessage : 'Không thể cấp mật khẩu tạm.');
    },
  });

  const columns: ColumnsType<PasswordHelpItem> = [
    {
      title: 'Người yêu cầu',
      dataIndex: 'displayName',
      key: 'displayName',
      render: (_value, row) => (
        <Space direction="vertical" size={0}>
          <Typography.Text strong>{row.displayName}</Typography.Text>
          <Typography.Text type="secondary" style={{ fontSize: 12 }}>
            {row.userName}
            {row.email ? ` · ${row.email}` : ''}
          </Typography.Text>
        </Space>
      ),
    },
    {
      title: 'Thời gian yêu cầu',
      dataIndex: 'requestedAtUtc',
      key: 'requestedAtUtc',
      render: (value) => formatSchoolDateTime(value),
    },
    {
      title: 'Trạng thái',
      dataIndex: 'status',
      key: 'status',
      render: (value) => <Tag color={statusColor[value]}>{statusLabel[value] ?? value}</Tag>,
    },
    {
      title: 'Thao tác',
      key: 'actions',
      render: (_value, row) => (
        <Space>
          <Button
            type="primary"
            onClick={() =>
              issueMutation.mutate({ userId: row.userId, rowVersion: row.rowVersion })
            }
            loading={issueMutation.isPending}
          >
            Cấp mật khẩu tạm
          </Button>
          <Button danger onClick={() => setRejectTarget(row)}>
            Từ chối
          </Button>
        </Space>
      ),
    },
  ];

  return (
    <Space direction="vertical" size={16} style={{ width: '100%' }}>
      {contextHolder}
      <Typography.Title level={3} style={{ marginTop: 0 }}>
        Yêu cầu hỗ trợ mật khẩu
      </Typography.Title>
      <Card>
        <Table<PasswordHelpItem>
          rowKey="requestId"
          loading={query.isLoading}
          dataSource={query.data?.items ?? []}
          columns={columns}
          pagination={false}
          locale={{
            emptyText: <Empty description="Không có yêu cầu đang chờ." />,
          }}
        />
      </Card>

      <Modal
        open={Boolean(issuedPassword)}
        title="Mật khẩu tạm đã được cấp"
        okText="Đã ghi nhận"
        cancelButtonProps={{ style: { display: 'none' } }}
        onOk={() => setIssuedPassword(null)}
        onCancel={() => setIssuedPassword(null)}
      >
        <Alert
          type="warning"
          showIcon
          message="Mật khẩu tạm chỉ hiển thị một lần. Vui lòng sao chép và chuyển cho người dùng qua kênh nội bộ."
          style={{ marginBottom: 12 }}
        />
        <Typography.Paragraph>
          Mật khẩu tạm: <Typography.Text code>{issuedPassword?.password}</Typography.Text>
        </Typography.Paragraph>
      </Modal>

      <Modal
        open={Boolean(rejectTarget)}
        title="Xác nhận từ chối yêu cầu"
        okText="Từ chối"
        okButtonProps={{ danger: true, loading: rejectMutation.isPending }}
        cancelText="Hủy"
        onCancel={() => setRejectTarget(null)}
        onOk={() => {
          if (rejectTarget) {
            rejectMutation.mutate({
              requestId: rejectTarget.requestId,
              rowVersion: rejectTarget.rowVersion,
            });
          }
        }}
      >
        <Typography.Paragraph>
          Yêu cầu của <Typography.Text strong>{rejectTarget?.displayName}</Typography.Text> sẽ được
          đánh dấu là "Không xác minh được".
        </Typography.Paragraph>
      </Modal>
    </Space>
  );
}
