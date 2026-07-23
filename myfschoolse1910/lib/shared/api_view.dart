import 'package:dio/dio.dart';
import 'package:flutter/material.dart';

import 'theme.dart';

Map<String, dynamic> asMap(dynamic value) =>
    Map<String, dynamic>.from(value as Map);

List<Map<String, dynamic>> asMapList(dynamic value) =>
    (value as List).map(asMap).toList();

String apiErrorMessage(Object error) {
  if (error is DioException) {
    final body = error.response?.data;
    if (body is Map) {
      final detail = body['detail']?.toString();
      if (detail != null && detail.isNotEmpty) return detail;
      final title = body['title']?.toString();
      if (title != null && title.isNotEmpty) return title;
    }
  }
  return 'Không thể kết nối tới hệ thống. Vui lòng thử lại.';
}

String formatDate(dynamic value) {
  final date = DateTime.tryParse(value?.toString() ?? '')?.toLocal();
  if (date == null) return value?.toString() ?? '—';
  return '${date.day.toString().padLeft(2, '0')}/'
      '${date.month.toString().padLeft(2, '0')}/${date.year}';
}

String formatDateTime(dynamic value) {
  final date = DateTime.tryParse(value?.toString() ?? '')?.toLocal();
  if (date == null) return value?.toString() ?? '—';
  return '${formatDate(date.toIso8601String())} '
      '${date.hour.toString().padLeft(2, '0')}:'
      '${date.minute.toString().padLeft(2, '0')}';
}

class LoadingView extends StatelessWidget {
  const LoadingView({super.key});

  @override
  Widget build(BuildContext context) =>
      const Center(child: CircularProgressIndicator());
}

class EmptyView extends StatelessWidget {
  final String message;
  final IconData icon;

  const EmptyView({
    super.key,
    required this.message,
    this.icon = Icons.inbox_outlined,
  });

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(32),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(icon, size: 52, color: AppTheme.textSecondary),
            const SizedBox(height: 12),
            Text(
              message,
              textAlign: TextAlign.center,
              style: const TextStyle(color: AppTheme.textSecondary),
            ),
          ],
        ),
      ),
    );
  }
}

class ErrorView extends StatelessWidget {
  final Object error;
  final VoidCallback onRetry;

  const ErrorView({super.key, required this.error, required this.onRetry});

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            const Icon(
              Icons.cloud_off_outlined,
              size: 52,
              color: AppTheme.error,
            ),
            const SizedBox(height: 12),
            Text(apiErrorMessage(error), textAlign: TextAlign.center),
            const SizedBox(height: 16),
            FilledButton.icon(
              onPressed: onRetry,
              icon: const Icon(Icons.refresh),
              label: const Text('Thử lại'),
            ),
          ],
        ),
      ),
    );
  }
}
