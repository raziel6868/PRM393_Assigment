import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../core/auth.dart';
import '../../shared/api_view.dart';
import '../../shared/theme.dart';

class LeaveRequestsScreen extends ConsumerStatefulWidget {
  const LeaveRequestsScreen({super.key});

  @override
  ConsumerState<LeaveRequestsScreen> createState() =>
      _LeaveRequestsScreenState();
}

class _LeaveRequestsScreenState extends ConsumerState<LeaveRequestsScreen> {
  late Future<List<Map<String, dynamic>>> _requests;

  @override
  void initState() {
    super.initState();
    _requests = _load();
  }

  Future<List<Map<String, dynamic>>> _load() async {
    final session = ref.read(authProvider).session;
    final client = ref.read(apiClientProvider);
    final response = session?.isTeacher == true
        ? await client.getTeacherLeaveRequests()
        : session?.isStudent == true
        ? await client.getStudentLeaveRequests()
        : await client.getLeaveRequests();
    return asMapList(asMap(response.data)['items'] ?? const []);
  }

  void _reload() => setState(() => _requests = _load());

  @override
  Widget build(BuildContext context) {
    final session = ref.watch(authProvider).session;
    return Scaffold(
      appBar: AppBar(title: const Text('Đơn xin nghỉ')),
      floatingActionButton: session?.isParent == true
          ? FloatingActionButton.extended(
              onPressed: () async {
                await context.push('/leave-requests/new');
                _reload();
              },
              icon: const Icon(Icons.add),
              label: const Text('Tạo đơn'),
            )
          : null,
      body: FutureBuilder<List<Map<String, dynamic>>>(
        future: _requests,
        builder: (context, snapshot) {
          if (snapshot.connectionState != ConnectionState.done) {
            return const LoadingView();
          }
          if (snapshot.hasError) {
            return ErrorView(error: snapshot.error!, onRetry: _reload);
          }
          final requests = snapshot.data ?? [];
          if (requests.isEmpty) {
            return const EmptyView(
              message: 'Chưa có đơn xin nghỉ.',
              icon: Icons.description_outlined,
            );
          }
          return RefreshIndicator(
            onRefresh: () async => _reload(),
            child: ListView.separated(
              padding: const EdgeInsets.fromLTRB(16, 16, 16, 96),
              itemCount: requests.length,
              separatorBuilder: (_, _) => const SizedBox(height: 10),
              itemBuilder: (context, index) {
                final request = requests[index];
                final status = request['status']?.toString() ?? 'pending';
                final color = _statusColor(status);
                return Card(
                  child: InkWell(
                    onTap: () => context.push(
                      '${session?.isTeacher == true ? '/teacher/leave-requests' : '/leave-requests'}/${request['id']}',
                      extra: request,
                    ),
                    borderRadius: BorderRadius.circular(12),
                    child: Padding(
                      padding: const EdgeInsets.all(16),
                      child: Row(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Container(
                            width: 4,
                            height: 72,
                            decoration: BoxDecoration(
                              color: color,
                              borderRadius: BorderRadius.circular(4),
                            ),
                          ),
                          const SizedBox(width: 14),
                          Expanded(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text(
                                  _categoryLabel(
                                    request['reasonCategory']?.toString(),
                                  ),
                                  style: const TextStyle(
                                    fontWeight: FontWeight.w700,
                                  ),
                                ),
                                const SizedBox(height: 6),
                                Text(
                                  '${formatDate(request['startDate'])} – '
                                  '${formatDate(request['endDate'])}',
                                ),
                                const SizedBox(height: 4),
                                Text(
                                  request['reason']?.toString() ?? '',
                                  maxLines: 2,
                                  overflow: TextOverflow.ellipsis,
                                  style: const TextStyle(
                                    color: AppTheme.textSecondary,
                                  ),
                                ),
                              ],
                            ),
                          ),
                          Chip(
                            label: Text(_statusLabel(status)),
                            backgroundColor: color.withValues(alpha: .12),
                            labelStyle: TextStyle(color: color),
                          ),
                        ],
                      ),
                    ),
                  ),
                );
              },
            ),
          );
        },
      ),
    );
  }

  Color _statusColor(String value) => switch (value) {
    'approved' => AppTheme.success,
    'rejected' => AppTheme.error,
    'cancelled' => AppTheme.textSecondary,
    _ => AppTheme.warning,
  };

  String _statusLabel(String value) => switch (value) {
    'approved' => 'Đã duyệt',
    'rejected' => 'Từ chối',
    'cancelled' => 'Đã huỷ',
    _ => 'Đang chờ',
  };

  String _categoryLabel(String? value) => switch (value) {
    'health' => 'Sức khoẻ',
    'family' => 'Gia đình',
    'academic' => 'Học tập',
    'personal' => 'Cá nhân',
    _ => 'Lý do khác',
  };
}
