import {
  Alert,
  Button,
  Card,
  Checkbox,
  Form,
  Input,
  Select,
  Space,
  Typography,
  message,
} from 'antd';
import { ArrowLeftOutlined, SendOutlined } from '@ant-design/icons';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link, useNavigate } from 'react-router-dom';
import {
  announcementsApi,
  type AnnouncementAudience,
  type DeliveryChannel,
} from '../../api/announcements';
import { isApiError } from '../../api/errors';
import { useAuth } from '../../app/auth-context';

type ComposerValues = {
  title: string;
  body: string;
  audience: AnnouncementAudience;
  targetClassId?: string;
  deliveryChannels: DeliveryChannel[];
};

const administratorAudienceOptions = [
  { value: 'schoolWide', label: 'Toàn trường' },
  { value: 'teacher', label: 'Toàn bộ giáo viên' },
  { value: 'parent', label: 'Toàn bộ phụ huynh' },
  { value: 'student', label: 'Toàn bộ học sinh' },
];

export function AnnouncementComposePage(): JSX.Element {
  const { state } = useAuth();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [messageApi, contextHolder] = message.useMessage();
  const isTeacherOnly =
    state.status === 'authenticated' &&
    state.user.roles.includes('teacher') &&
    !state.user.roles.includes('administrator');

  const classesQuery = useQuery({
    queryKey: ['me', 'classes', 'announcement-composer'],
    queryFn: announcementsApi.myClasses,
    enabled: isTeacherOnly,
  });

  const sendMutation = useMutation({
    mutationFn: async (values: ComposerValues) => {
      const draft = await announcementsApi.create({
        title: values.title.trim(),
        body: values.body.trim(),
        audience: values.audience,
        targetClassId:
          values.audience === 'class' ? values.targetClassId ?? null : null,
        imageUrl: null,
      });
      await announcementsApi.publish(
        draft.id,
        draft.rowVersion,
        values.deliveryChannels,
      );
      return draft;
    },
    onSuccess: () => {
      messageApi.success('Thông báo đã được phát hành.');
      void queryClient.invalidateQueries({ queryKey: ['announcements'] });
      navigate('/announcements', { replace: true });
    },
    onError: (error) => {
      messageApi.error(
        isApiError(error) ? error.userMessage : 'Không thể gửi thông báo.',
      );
    },
  });

  const classOptions = (classesQuery.data ?? []).map((item) => ({
    value: item.classId,
    label: `${item.classDisplayName} · ${item.subjectDisplayName}`,
  }));

  return (
    <Space direction="vertical" size={20} style={{ width: '100%' }}>
      {contextHolder}
      <Space>
        <Link to="/announcements">
          <Button icon={<ArrowLeftOutlined />}>Quay lại</Button>
        </Link>
        <div>
          <Typography.Title level={3} style={{ margin: 0 }}>
            Soạn thông báo & Email
          </Typography.Title>
          <Typography.Text type="secondary">
            Nội dung được gửi qua ứng dụng và Email theo kênh đã chọn.
          </Typography.Text>
        </div>
      </Space>

      {isTeacherOnly && !classesQuery.isLoading && classOptions.length === 0 && (
        <Alert
          type="warning"
          showIcon
          message="Bạn chưa được phân công lớp/môn nên chưa thể gửi thông báo."
        />
      )}

      <Card>
        <Form<ComposerValues>
          layout="vertical"
          initialValues={{
            audience: isTeacherOnly ? 'class' : 'schoolWide',
            deliveryChannels: ['portalApp'],
          }}
          onFinish={(values) => sendMutation.mutate(values)}
          requiredMark={false}
        >
          <Form.Item
            name="title"
            label="Tiêu đề"
            rules={[
              { required: true, message: 'Vui lòng nhập tiêu đề.' },
              { max: 100, message: 'Tiêu đề tối đa 100 ký tự.' },
            ]}
          >
            <Input placeholder="Ví dụ: Lịch sinh hoạt ngoại khóa tháng 10" />
          </Form.Item>

          <Form.Item
            name="audience"
            label="Đối tượng nhận"
            rules={[{ required: true, message: 'Vui lòng chọn đối tượng nhận.' }]}
          >
            <Select
              disabled={isTeacherOnly}
              options={
                isTeacherOnly
                  ? [{ value: 'class', label: 'Lớp được phân công' }]
                  : administratorAudienceOptions
              }
            />
          </Form.Item>

          {isTeacherOnly && (
            <Form.Item
              name="targetClassId"
              label="Lớp nhận thông báo"
              rules={[{ required: true, message: 'Vui lòng chọn lớp.' }]}
            >
              <Select
                loading={classesQuery.isLoading}
                options={classOptions}
                placeholder="Chọn lớp và môn được phân công"
              />
            </Form.Item>
          )}

          <Form.Item
            name="body"
            label="Nội dung"
            rules={[
              { required: true, message: 'Vui lòng nhập nội dung.' },
              { max: 4000, message: 'Nội dung tối đa 4000 ký tự.' },
            ]}
          >
            <Input.TextArea
              rows={12}
              showCount
              maxLength={4000}
              placeholder="Nhập nội dung thông báo bằng tiếng Việt..."
            />
          </Form.Item>

          <Form.Item
            name="deliveryChannels"
            label="Kênh gửi"
            rules={[
              {
                validator: (_, value: DeliveryChannel[] | undefined) =>
                  value && value.length > 0
                    ? Promise.resolve()
                    : Promise.reject(new Error('Chọn ít nhất một kênh gửi.')),
              },
            ]}
          >
            <Checkbox.Group
              options={[
                { value: 'portalApp', label: 'Ứng dụng MyFSchool' },
                { value: 'email', label: 'Email qua Gmail SMTP' },
              ]}
            />
          </Form.Item>

          <Button
            type="primary"
            htmlType="submit"
            icon={<SendOutlined />}
            loading={sendMutation.isPending}
            disabled={
              sendMutation.isPending ||
              (isTeacherOnly && !classesQuery.isLoading && classOptions.length === 0)
            }
          >
            Gửi thông báo
          </Button>
        </Form>
      </Card>
    </Space>
  );
}
