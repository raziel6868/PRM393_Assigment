import { Alert, Button, Card, Form, Input, Typography } from 'antd';
import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../../app/auth-context';
import { isApiError } from '../../api/errors';
import { brandOrangeDark } from '../../shared/theme';

type FormValues = { currentPassword: string; newPassword: string; confirmation: string };

const passwordMinLength = 12;

export function ChangeTemporaryPasswordPage(): JSX.Element {
  const { state, changeTemporaryPassword } = useAuth();
  const navigate = useNavigate();
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const onSubmit = async (values: FormValues): Promise<void> => {
    setError(null);
    if (values.newPassword === values.currentPassword) {
      setError('Mật khẩu mới phải khác mật khẩu tạm hiện tại.');
      return;
    }
    setSubmitting(true);
    try {
      await changeTemporaryPassword({
        currentPassword: values.currentPassword,
        newPassword: values.newPassword,
        confirmation: values.confirmation,
      });
      navigate('/login', { replace: true });
    } catch (err) {
      if (isApiError(err)) {
        setError(err.userMessage);
      } else {
        setError('Không thể đổi mật khẩu. Vui lòng thử lại.');
      }
    } finally {
      setSubmitting(false);
    }
  };

  const displayName = state.status === 'restricted' ? state.user.displayName : '';

  return (
    <div
      style={{
        minHeight: '100vh',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        background: '#FFF3E0',
        padding: 16,
      }}
    >
      <Card style={{ maxWidth: 480, width: '100%' }}>
        <Typography.Title level={3} style={{ marginTop: 0, color: brandOrangeDark }}>
          Đổi mật khẩu tạm thời
        </Typography.Title>
        <Typography.Paragraph>
          Xin chào <strong>{displayName}</strong>. Vui lòng đặt mật khẩu mới để tiếp tục sử dụng hệ thống.
        </Typography.Paragraph>

        {error && (
          <Alert
            type="error"
            showIcon
            message={error}
            style={{ marginBottom: 16 }}
            role="alert"
          />
        )}

        <Form<FormValues> layout="vertical" onFinish={onSubmit} requiredMark={false}>
          <Form.Item
            label="Mật khẩu tạm hiện tại"
            name="currentPassword"
            rules={[{ required: true, message: 'Vui lòng nhập mật khẩu tạm hiện tại.' }]}
          >
            <Input.Password autoComplete="current-password" />
          </Form.Item>
          <Form.Item
            label="Mật khẩu mới"
            name="newPassword"
            rules={[
              { required: true, message: 'Vui lòng nhập mật khẩu mới.' },
              { min: passwordMinLength, message: `Mật khẩu mới phải có ít nhất ${passwordMinLength} ký tự.` },
            ]}
          >
            <Input.Password autoComplete="new-password" />
          </Form.Item>
          <Form.Item
            label="Xác nhận mật khẩu mới"
            name="confirmation"
            dependencies={['newPassword']}
            rules={[
              { required: true, message: 'Vui lòng xác nhận mật khẩu mới.' },
              ({ getFieldValue }) => ({
                validator(_, value) {
                  if (!value || getFieldValue('newPassword') === value) {
                    return Promise.resolve();
                  }
                  return Promise.reject(new Error('Mật khẩu xác nhận không khớp.'));
                },
              }),
            ]}
          >
            <Input.Password autoComplete="new-password" />
          </Form.Item>
          <Button block type="primary" htmlType="submit" loading={submitting} disabled={submitting}>
            Đổi mật khẩu
          </Button>
        </Form>
      </Card>
    </div>
  );
}
