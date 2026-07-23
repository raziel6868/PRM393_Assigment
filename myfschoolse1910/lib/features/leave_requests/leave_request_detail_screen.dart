import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/auth.dart';
import '../../shared/api_view.dart';
import '../../shared/theme.dart';

class LeaveRequestDetailScreen extends ConsumerStatefulWidget {
  final String requestId;
  final Map<String, dynamic>? initial;

  const LeaveRequestDetailScreen({
    super.key,
    required this.requestId,
    this.initial,
  });

  @override
  ConsumerState<LeaveRequestDetailScreen> createState() =>
      _LeaveRequestDetailScreenState();
}

class _LeaveRequestDetailScreenState
    extends ConsumerState<LeaveRequestDetailScreen> {
  late Future<Map<String, dynamic>> _request;
  bool _submitting = false;

  @override
  void initState() {
    super.initState();
    _request = _load();
  }

  Future<Map<String, dynamic>> _load() async {
    final session = ref.read(authProvider).session;
    if (session?.isStudent == true && widget.initial != null) {
      return widget.initial!;
    }
    final response = session?.isTeacher == true
        ? await ref
              .read(apiClientProvider)
              .getTeacherLeaveRequestDetail(widget.requestId)
        : await ref
              .read(apiClientProvider)
              .getLeaveRequestDetail(widget.requestId);
    return asMap(response.data);
  }

  void _reload() => setState(() => _request = _load());

  Future<void> _cancel(Map<String, dynamic> request) async {
    await _runAction(
      () => ref
          .read(apiClientProvider)
          .cancelLeaveRequest(
            widget.requestId,
            request['rowVersion'].toString(),
          ),
      'Đã huỷ đơn xin nghỉ.',
    );
  }

  Future<void> _decide(
    Map<String, dynamic> request, {
    required bool approve,
  }) async {
    String? note;
    if (!approve) {
      note = await _askRejectionReason();
      if (note == null) return;
    }
    await _runAction(
      () => ref
          .read(apiClientProvider)
          .decideLeaveRequest(
            widget.requestId,
            approve: approve,
            decisionNote: note,
            rowVersion: request['rowVersion'].toString(),
          ),
      approve ? 'Đã duyệt đơn.' : 'Đã từ chối đơn.',
    );
  }

  Future<String?> _askRejectionReason() async {
    final controller = TextEditingController();
    final result = await showDialog<String>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Lý do từ chối'),
        content: TextField(
          controller: controller,
          minLines: 2,
          maxLines: 4,
          autofocus: true,
          decoration: const InputDecoration(
            hintText: 'Nhập lý do để phụ huynh biết...',
          ),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context),
            child: const Text('Đóng'),
          ),
          FilledButton(
            onPressed: () {
              final value = controller.text.trim();
              if (value.isNotEmpty) Navigator.pop(context, value);
            },
            child: const Text('Xác nhận'),
          ),
        ],
      ),
    );
    controller.dispose();
    return result;
  }

  Future<void> _runAction(
    Future<dynamic> Function() action,
    String successMessage,
  ) async {
    setState(() => _submitting = true);
    try {
      await action();
      if (mounted) {
        ScaffoldMessenger.of(
          context,
        ).showSnackBar(SnackBar(content: Text(successMessage)));
        _reload();
      }
    } catch (error) {
      if (mounted) {
        ScaffoldMessenger.of(
          context,
        ).showSnackBar(SnackBar(content: Text(apiErrorMessage(error))));
      }
    } finally {
      if (mounted) setState(() => _submitting = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final session = ref.watch(authProvider).session;
    return Scaffold(
      appBar: AppBar(title: const Text('Chi tiết đơn')),
      body: FutureBuilder<Map<String, dynamic>>(
        future: _request,
        builder: (context, snapshot) {
          if (snapshot.connectionState != ConnectionState.done) {
            return const LoadingView();
          }
          if (snapshot.hasError) {
            return ErrorView(error: snapshot.error!, onRetry: _reload);
          }
          final request = snapshot.data!;
          final pending = request['status'] == 'pending';
          return ListView(
            padding: const EdgeInsets.all(16),
            children: [
              Card(
                child: Padding(
                  padding: const EdgeInsets.all(18),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Row(
                        children: [
                          const Expanded(
                            child: Text(
                              'Đơn xin nghỉ học',
                              style: TextStyle(
                                fontSize: 20,
                                fontWeight: FontWeight.w800,
                              ),
                            ),
                          ),
                          Chip(
                            label: Text(switch (request['status']) {
                              'approved' => 'Đã duyệt',
                              'rejected' => 'Từ chối',
                              'cancelled' => 'Đã huỷ',
                              _ => 'Đang chờ',
                            }),
                          ),
                        ],
                      ),
                      const Divider(height: 28),
                      Text(
                        '${formatDate(request['startDate'])} – '
                        '${formatDate(request['endDate'])}',
                        style: const TextStyle(fontWeight: FontWeight.w700),
                      ),
                      const SizedBox(height: 12),
                      Text(request['reason']?.toString() ?? ''),
                      const SizedBox(height: 12),
                      Text(
                        'Gửi lúc ${formatDateTime(request['createdAtUtc'])}',
                        style: const TextStyle(color: AppTheme.textSecondary),
                      ),
                    ],
                  ),
                ),
              ),
              if ((request['decisionNote']?.toString() ?? '').isNotEmpty) ...[
                const SizedBox(height: 12),
                Card(
                  color: AppTheme.orangeTint,
                  child: Padding(
                    padding: const EdgeInsets.all(16),
                    child: Text(
                      'Phản hồi của giáo viên:\n'
                      '${request['decisionNote']}',
                    ),
                  ),
                ),
              ],
              if (pending && session?.isParent == true) ...[
                const SizedBox(height: 18),
                OutlinedButton.icon(
                  onPressed: _submitting ? null : () => _cancel(request),
                  icon: const Icon(Icons.cancel_outlined),
                  label: const Text('Huỷ đơn'),
                ),
              ],
              if (pending && session?.isTeacher == true) ...[
                const SizedBox(height: 18),
                Row(
                  children: [
                    Expanded(
                      child: OutlinedButton(
                        onPressed: _submitting
                            ? null
                            : () => _decide(request, approve: false),
                        child: const Text('Từ chối'),
                      ),
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: FilledButton(
                        onPressed: _submitting
                            ? null
                            : () => _decide(request, approve: true),
                        child: const Text('Duyệt đơn'),
                      ),
                    ),
                  ],
                ),
              ],
            ],
          );
        },
      ),
    );
  }
}
