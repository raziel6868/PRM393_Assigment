import {
  Alert,
  Button,
  Card,
  Empty,
  Space,
  Steps,
  Table,
  Tag,
  Typography,
  Upload,
  message,
} from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useEffect, useMemo, useState } from 'react';
import { InboxOutlined, DownloadOutlined } from '@ant-design/icons';
import {
  importsApi,
  type ImportBatchSummary,
  type ImportValidation,
  type ImportValidationError,
} from '../../api/imports';
import { isApiError } from '../../api/errors';
import { formatSchoolDateTime } from '../../shared/format';

type StepKey = 'upload' | 'validate' | 'commit' | 'result';

const stepIndex: Record<StepKey, number> = {
  upload: 0,
  validate: 1,
  commit: 2,
  result: 3,
};

const stepLabel: Record<StepKey, string> = {
  upload: 'Tải tệp lên',
  validate: 'Kiểm tra',
  commit: 'Xác nhận',
  result: 'Hoàn tất',
};

export function ImportsPage(): JSX.Element {
  const queryClient = useQueryClient();
  const [messageApi, contextHolder] = message.useMessage();
  const [batch, setBatch] = useState<ImportBatchSummary | null>(null);
  const [validation, setValidation] = useState<ImportValidation | null>(null);

  const templateInfoQuery = useQuery({
    queryKey: ['imports', 'template-info'],
    queryFn: () => importsApi.templateInfo(),
  });

  const resultQuery = useQuery({
    queryKey: ['imports', 'result', batch?.batchId],
    queryFn: () => importsApi.result(batch!.batchId),
    enabled: Boolean(batch),
    refetchInterval: (query) => {
      const data = query.state.data;
      if (!data) return false;
      if (data.status === 'committed' || data.status === 'failed' || data.status === 'rejected') {
        return false;
      }
      return 2000;
    },
  });

  useEffect(() => {
    if (resultQuery.data) {
      setBatch(resultQuery.data);
    }
  }, [resultQuery.data]);

  const uploadMutation = useMutation({
    mutationFn: (file: File) => importsApi.upload(file),
    onSuccess: (response) => {
      setBatch(response);
      setValidation(null);
      messageApi.success('Tệp đã được tải lên. Tiếp theo: kiểm tra.');
      void queryClient.invalidateQueries({ queryKey: ['imports'] });
    },
    onError: (err) => {
      messageApi.error(isApiError(err) ? err.userMessage : 'Không thể tải lên tệp.');
    },
  });

  const validateMutation = useMutation({
    mutationFn: (batchId: string) => importsApi.validate(batchId),
    onSuccess: (response) => {
      setValidation(response);
      setBatch((current) =>
        current
          ? {
              ...current,
              status: 'validated',
              hasBlockingErrors: response.hasBlockingErrors,
              rowCount: response.totalRowCount,
            }
          : current,
      );
      if (response.hasBlockingErrors) {
        messageApi.warning('Phát hiện lỗi chặn. Vui lòng sửa tệp và tải lên lại.');
      } else {
        messageApi.success('Tệp hợp lệ. Bạn có thể xác nhận lưu.');
      }
    },
    onError: (err) => {
      messageApi.error(isApiError(err) ? err.userMessage : 'Không thể kiểm tra tệp.');
    },
  });

  const commitMutation = useMutation({
    mutationFn: (batchId: string) => importsApi.commit(batchId),
    onSuccess: (response) => {
      setBatch(response);
      messageApi.success('Đã lưu lô nhập vào hệ thống.');
      void queryClient.invalidateQueries({ queryKey: ['imports'] });
    },
    onError: (err) => {
      messageApi.error(isApiError(err) ? err.userMessage : 'Không thể lưu lô nhập.');
    },
  });

  const currentStep: StepKey = useMemo(() => {
    if (!batch) return 'upload';
    if (batch.status === 'committed') return 'result';
    if (batch.status === 'failed' || batch.status === 'rejected') return 'validate';
    if (validation) return 'commit';
    return 'validate';
  }, [batch, validation]);

  const errorColumns: ColumnsType<ImportValidationError> = [
    { title: 'Sheet', dataIndex: 'sheet', key: 'sheet', width: 160 },
    { title: 'Dòng', dataIndex: 'rowNumber', key: 'rowNumber', width: 80 },
    { title: 'Cột', dataIndex: 'column', key: 'column', width: 160 },
    {
      title: 'Mức độ',
      dataIndex: 'severity',
      key: 'severity',
      width: 120,
      render: (value) => (
        <Tag color={value === 'error' ? 'red' : 'orange'}>
          {value === 'error' ? 'Lỗi chặn' : 'Cảnh báo'}
        </Tag>
      ),
    },
    { title: 'Mã lỗi', dataIndex: 'code', key: 'code', width: 200 },
    { title: 'Mô tả', dataIndex: 'message', key: 'message' },
  ];

  const handleDownloadTemplate = async (): Promise<void> => {
    try {
      await importsApi.downloadTemplate();
    } catch (err) {
      messageApi.error(isApiError(err) ? err.userMessage : 'Không thể tải tệp mẫu.');
    }
  };

  const handleReset = (): void => {
    setBatch(null);
    setValidation(null);
  };

  return (
    <Space direction="vertical" size={16} style={{ width: '100%' }}>
      {contextHolder}
      <Typography.Title level={3} style={{ marginTop: 0 }}>
        Nhập liệu danh sách
      </Typography.Title>

      <Card>
        <Steps
          current={stepIndex[currentStep]}
          items={(['upload', 'validate', 'commit', 'result'] as StepKey[]).map((key) => ({
            title: stepLabel[key],
          }))}
        />
      </Card>

      {currentStep === 'upload' && (
        <Card title="Bước 1 — Tải tệp Excel lên">
          <Space direction="vertical" size={16} style={{ width: '100%' }}>
            <Alert
              type="info"
              showIcon
              message="Hỗ trợ tệp .xlsx theo mẫu chuẩn của hệ thống."
              description={
                templateInfoQuery.data
                  ? `Phiên bản mẫu: ${templateInfoQuery.data.templateVersion}. Các sheet bắt buộc: ${templateInfoQuery.data.sheets.join(', ')}.`
                  : 'Đang tải thông tin mẫu...'
              }
            />
            <Upload.Dragger
              accept=".xlsx"
              multiple={false}
              showUploadList={false}
              beforeUpload={(file) => {
                uploadMutation.mutate(file);
                return false;
              }}
              disabled={uploadMutation.isPending}
            >
              <p className="ant-upload-drag-icon">
                <InboxOutlined />
              </p>
              <p className="ant-upload-text">Kéo thả tệp .xlsx vào đây hoặc bấm để chọn</p>
              <p className="ant-upload-hint">Tối đa 10 MB.</p>
            </Upload.Dragger>
            <Button icon={<DownloadOutlined />} onClick={handleDownloadTemplate} loading={templateInfoQuery.isLoading}>
              Tải tệp mẫu
            </Button>
          </Space>
        </Card>
      )}

      {batch && currentStep === 'validate' && (
        <Card
          title={`Bước 2 — Kiểm tra lô ${batch.fileName}`}
          extra={<Button onClick={handleReset}>Tải tệp khác</Button>}
        >
          <Space direction="vertical" size={16} style={{ width: '100%' }}>
            <Typography.Paragraph>
              Tệp <Typography.Text strong>{batch.fileName}</Typography.Text> đã sẵn sàng để kiểm
              tra. Bấm "Bắt đầu kiểm tra" để hệ thống rà soát các sheet, tiêu đề và giá trị theo mẫu.
            </Typography.Paragraph>
            <Button
              type="primary"
              loading={validateMutation.isPending}
              onClick={() => validateMutation.mutate(batch.batchId)}
            >
              Bắt đầu kiểm tra
            </Button>
          </Space>
        </Card>
      )}

      {validation && (
        <Card title="Kết quả kiểm tra">
          <Space direction="vertical" size={12} style={{ width: '100%' }}>
            <Space wrap>
              <Tag color="blue">Tổng dòng: {validation.totalRowCount}</Tag>
              <Tag color="green">Hợp lệ: {validation.validRowCount}</Tag>
              <Tag color="orange">Cảnh báo: {validation.warningRowCount}</Tag>
              <Tag color="red">Lỗi chặn: {validation.errorRowCount}</Tag>
            </Space>
            {validation.hasBlockingErrors ? (
              <Alert
                type="error"
                showIcon
                message="Có lỗi chặn. Vui lòng sửa tệp và tải lên lại trước khi tiếp tục."
              />
            ) : (
              <Alert type="success" showIcon message="Tệp không có lỗi chặn. Bạn có thể xác nhận lưu." />
            )}
            <Table<ImportValidationError>
              rowKey={(_, index) => String(index)}
              dataSource={validation.errors}
              columns={errorColumns}
              pagination={{ pageSize: 10 }}
              locale={{
                emptyText: <Empty description="Không có lỗi nào được phát hiện." />,
              }}
            />
          </Space>
        </Card>
      )}

      {validation && !validation.hasBlockingErrors && currentStep !== 'result' && (
        <Card title="Bước 3 — Xác nhận lưu">
          <Space direction="vertical" size={12} style={{ width: '100%' }}>
            <Typography.Paragraph>
              Hệ thống sẽ tạo/cập nhật các bản ghi người dùng, hồ sơ và quan hệ theo dữ liệu trong
              tệp. Vui lòng xác nhận để lưu.
            </Typography.Paragraph>
            <Button
              type="primary"
              loading={commitMutation.isPending}
              onClick={() => batch && commitMutation.mutate(batch.batchId)}
            >
              Xác nhận lưu lô nhập
            </Button>
          </Space>
        </Card>
      )}

      {batch && currentStep === 'result' && (
        <Card title="Bước 4 — Kết quả">
          <Space direction="vertical" size={12} style={{ width: '100%' }}>
            <Typography.Paragraph>
              Lô nhập <Typography.Text code>{batch.batchId}</Typography.Text> đã được lưu thành công.
            </Typography.Paragraph>
            <Space wrap>
              <Tag color="green">Đã tạo người dùng: {batch.createdUserCount}</Tag>
              <Tag color="blue">Đã cập nhật: {batch.updatedUserCount}</Tag>
              <Tag color="cyan">Hồ sơ: {batch.createdProfileCount}</Tag>
              <Tag color="purple">Quan hệ: {batch.createdLinkCount}</Tag>
              <Tag color="magenta">Phân công: {batch.createdAssignmentCount}</Tag>
              <Tag color="geekblue">Ghi danh: {batch.createdEnrollmentCount}</Tag>
            </Space>
            <Space>
              <Button onClick={handleDownloadTemplate}>Tải tệp mẫu</Button>
              <Button type="primary" onClick={handleReset}>
                Bắt đầu lô nhập mới
              </Button>
            </Space>
            <Typography.Paragraph type="secondary">
              Thời điểm lưu: {formatSchoolDateTime(batch.committedAtUtc)}
            </Typography.Paragraph>
          </Space>
        </Card>
      )}
    </Space>
  );
}
