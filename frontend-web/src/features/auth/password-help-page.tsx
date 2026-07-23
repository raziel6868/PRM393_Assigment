import { Alert, Button, Card, Form, Input, Result, Typography } from 'antd';
import { useState } from 'react';
import { Link } from 'react-router-dom';
import { authApi } from '../../api/auth';
import { isApiError } from '../../api/errors';
import { brandOrangeDark } from '../../shared/theme';

type FormValues = { emailOrUserName: string };

export function PasswordHelpPage(): JSX.Element {
  const [submitted, setSubmitted] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const onSubmit = async (values: FormValues): Promise<void> => {
    setError(null);
    setSubmitting(true);
    try {
      await authApi.submitPasswordHelp({ emailOrUserName: values.emailOrUserName.trim() });
      setSubmitted(true);
    } catch (err) {
      if (isApiError(err) && err.status === 429) {
        setError('Bạn đã gửi quá nhiều yêu cầu. Vui lòng chờ một lát rồi thử lại.');
        return;
      }
      setError('Không thể gửi yêu cầu. Vui lòng thử lại sau.');
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
        background: '#FFF3E0',
        padding: 16,
      }}
    >
      <Card style={{ maxWidth: 480, width: '100%' }}>
        <Typography.Title level={3} style={{ marginTop: 0, color: brandOrangeDark }}>
          Yêu cầu hỗ trợ mật khẩu
        </Typography.Title>
        <Typography.Paragraph>
          Nhập email hoặc mã tài khoản của bạn. Nhà trường sẽ xác minh danh tính qua quy trình nội bộ
          và cấp mật khẩu tạm thời.
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

        {submitted ? (
          <Result
            status="success"
            title="Đã gửi yêu cầu"
            subTitle="Nếu thông tin phù hợp, nhà trường sẽ liên hệ để hỗ trợ bạn."
            extra={[
              <Link key="back" to="/login">
                <Button type="primary">Quay lại đăng nhập</Button>
              </Link>,
            ]}
          />
        ) : (
          <Form<FormValues> layout="vertical" onFinish={onSubmit} requiredMark={false}>
            <Form.Item
              label="Email hoặc mã tài khoản"
              name="emailOrUserName"
              rules={[{ required: true, message: 'Vui lòng nhập email hoặc mã tài khoản.' }]}
            >
              <Input autoComplete="username" />
            </Form.Item>
            <Button block type="primary" htmlType="submit" loading={submitting} disabled={submitting}>
              Gửi yêu cầu hỗ trợ
            </Button>
          </Form>
        )}

        <div style={{ textAlign: 'center', marginTop: 16 }}>
          <Link to="/login">Quay lại đăng nhập</Link>
        </div>
      </Card>
    </div>
  );
}
