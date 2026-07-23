import { Alert, Button, Card, Form, Input, Typography } from 'antd';
import { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { LockOutlined, MailOutlined } from '@ant-design/icons';
import { useAuth } from '../../app/auth-context';
import { isApiError } from '../../api/errors';
import { brandOrange, brandOrangeDark } from '../../shared/theme';

type FormValues = { emailOrUserName: string; password: string };

export function LoginPage(): JSX.Element {
  const { signIn } = useAuth();
  const navigate = useNavigate();
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const onSubmit = async (values: FormValues): Promise<void> => {
    setError(null);
    setSubmitting(true);
    try {
      await signIn({ emailOrUserName: values.emailOrUserName.trim(), password: values.password, clientType: 'web' });
      navigate('/dashboard', { replace: true });
    } catch (err) {
      if (isApiError(err) && err.code === 'temporaryPasswordExpired') {
        navigate('/password-help', { replace: true });
        return;
      }
      setError(isApiError(err) ? err.userMessage : 'Không thể đăng nhập.');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div
      style={{
        minHeight: '100vh',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        background: 'linear-gradient(135deg, #FFF3E0 0%, #FFE0B2 100%)',
        padding: 16,
      }}
    >
      <Card style={{ maxWidth: 440, width: '100%' }}>
        <div style={{ textAlign: 'center', marginBottom: 24 }}>
          <div
            style={{
              width: 64,
              height: 64,
              borderRadius: 16,
              background: brandOrange,
              color: '#fff',
              display: 'inline-flex',
              alignItems: 'center',
              justifyContent: 'center',
              fontWeight: 700,
              fontSize: 28,
              marginBottom: 12,
            }}
            aria-hidden
          >
            F
          </div>
          <Typography.Title level={3} style={{ margin: 0, color: brandOrangeDark }}>
            MyFSchool
          </Typography.Title>
          <Typography.Paragraph type="secondary" style={{ marginTop: 4 }}>
            Đăng nhập hệ thống quản trị
          </Typography.Paragraph>
        </div>

        {error && (
          <Alert
            type="error"
            showIcon
            message={error}
            style={{ marginBottom: 16 }}
            role="alert"
          />
        )}

        <Form<FormValues>
          layout="vertical"
          onFinish={onSubmit}
          requiredMark={false}
          autoComplete="on"
        >
          <Form.Item
            label="Email hoặc mã tài khoản"
            name="emailOrUserName"
            rules={[
              { required: true, message: 'Vui lòng nhập email hoặc mã tài khoản.' },
              { max: 256, message: 'Email hoặc mã tài khoản không hợp lệ.' },
            ]}
          >
            <Input prefix={<MailOutlined />} autoComplete="username" />
          </Form.Item>
          <Form.Item
            label="Mật khẩu"
            name="password"
            rules={[{ required: true, message: 'Vui lòng nhập mật khẩu.' }]}
          >
            <Input.Password prefix={<LockOutlined />} autoComplete="current-password" />
          </Form.Item>
          <Button
            block
            type="primary"
            htmlType="submit"
            loading={submitting}
            disabled={submitting}
          >
            Đăng nhập
          </Button>
        </Form>

        <div style={{ textAlign: 'center', marginTop: 16 }}>
          <Link to="/password-help">Quên mật khẩu?</Link>
        </div>
      </Card>
    </div>
  );
}
