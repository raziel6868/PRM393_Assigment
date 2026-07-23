import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/auth.dart';
import '../../shared/api_view.dart';
import '../../shared/theme.dart';

class AttendanceScreen extends ConsumerWidget {
  const AttendanceScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final session = ref.watch(authProvider).session;
    return session?.isTeacher == true
        ? const _TeacherAttendanceView()
        : const _AttendanceHistoryView();
  }
}

class _AttendanceHistoryView extends ConsumerStatefulWidget {
  const _AttendanceHistoryView();

  @override
  ConsumerState<_AttendanceHistoryView> createState() =>
      _AttendanceHistoryViewState();
}

class _AttendanceHistoryViewState
    extends ConsumerState<_AttendanceHistoryView> {
  List<Map<String, dynamic>> _children = [];
  String? _studentProfileId;
  late Future<List<Map<String, dynamic>>> _history;

  @override
  void initState() {
    super.initState();
    _history = _load();
  }

  Future<List<Map<String, dynamic>>> _load() async {
    if (ref.read(authProvider).session?.isParent == true && _children.isEmpty) {
      final response = await ref.read(apiClientProvider).getClasses();
      final seen = <String>{};
      _children = asMapList(response.data)
          .where((item) => seen.add(item['studentProfileId']?.toString() ?? ''))
          .toList();
      _studentProfileId ??= _children.isEmpty
          ? null
          : _children.first['studentProfileId']?.toString();
    }
    final response = await ref
        .read(apiClientProvider)
        .getAttendanceHistory(studentProfileId: _studentProfileId);
    return asMapList(asMap(response.data)['items'] ?? const []);
  }

  void _reload() => setState(() => _history = _load());

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Điểm danh')),
      body: Column(
        children: [
          if (_children.length > 1)
            Padding(
              padding: const EdgeInsets.all(16),
              child: DropdownButtonFormField<String>(
                initialValue: _studentProfileId,
                decoration: const InputDecoration(labelText: 'Học sinh'),
                items: _children
                    .map(
                      (child) => DropdownMenuItem(
                        value: child['studentProfileId']?.toString(),
                        child: Text(child['studentDisplayName'].toString()),
                      ),
                    )
                    .toList(),
                onChanged: (value) {
                  _studentProfileId = value;
                  _reload();
                },
              ),
            ),
          Expanded(
            child: FutureBuilder<List<Map<String, dynamic>>>(
              future: _history,
              builder: (context, snapshot) {
                if (snapshot.connectionState != ConnectionState.done) {
                  return const LoadingView();
                }
                if (snapshot.hasError) {
                  return ErrorView(error: snapshot.error!, onRetry: _reload);
                }
                final items = snapshot.data ?? [];
                if (items.isEmpty) {
                  return const EmptyView(
                    message: 'Chưa có dữ liệu điểm danh.',
                    icon: Icons.fact_check_outlined,
                  );
                }
                return RefreshIndicator(
                  onRefresh: () async => _reload(),
                  child: ListView.separated(
                    padding: const EdgeInsets.all(16),
                    itemCount: items.length,
                    separatorBuilder: (_, _) => const SizedBox(height: 8),
                    itemBuilder: (context, index) {
                      final item = items[index];
                      final status = item['status']?.toString() ?? 'unmarked';
                      return Card(
                        child: ListTile(
                          leading: Icon(
                            _statusIcon(status),
                            color: _statusColor(status),
                          ),
                          title: Text(
                            '${formatDate(item['attendanceDate'])} · '
                            '${item['session'] == 'morning' ? 'Buổi sáng' : 'Buổi chiều'}',
                          ),
                          subtitle: Text(
                            '${item['classDisplayName']}'
                            '${item['note'] == null ? '' : '\n${item['note']}'}',
                          ),
                          trailing: Text(
                            _statusLabel(status),
                            style: TextStyle(
                              color: _statusColor(status),
                              fontWeight: FontWeight.w700,
                            ),
                          ),
                        ),
                      );
                    },
                  ),
                );
              },
            ),
          ),
        ],
      ),
    );
  }
}

class _TeacherAttendanceView extends ConsumerStatefulWidget {
  const _TeacherAttendanceView();

  @override
  ConsumerState<_TeacherAttendanceView> createState() =>
      _TeacherAttendanceViewState();
}

class _TeacherAttendanceViewState
    extends ConsumerState<_TeacherAttendanceView> {
  late Future<List<Map<String, dynamic>>> _classes;
  Future<Map<String, dynamic>>? _roster;
  String? _classId;
  String _session = 'morning';
  final DateTime _date = DateTime.now();
  final Map<String, String> _statuses = {};
  bool _saving = false;

  @override
  void initState() {
    super.initState();
    _classes = _loadClasses();
  }

  Future<List<Map<String, dynamic>>> _loadClasses() async {
    final response = await ref.read(apiClientProvider).getClasses();
    final seen = <String>{};
    final classes = asMapList(
      response.data,
    ).where((item) => seen.add(item['classId'].toString())).toList();
    if (classes.isNotEmpty) {
      _classId ??= classes.first['classId']?.toString();
      _roster = _loadRoster();
    }
    return classes;
  }

  Future<Map<String, dynamic>> _loadRoster() async {
    final response = await ref
        .read(apiClientProvider)
        .getTeacherAttendanceRoster(_classId!, date: _date, session: _session);
    final roster = asMap(response.data);
    _statuses
      ..clear()
      ..addEntries(
        asMapList(roster['entries'] ?? const []).map(
          (item) => MapEntry(
            item['studentProfileId'].toString(),
            item['status'] == 'unmarked'
                ? 'present'
                : item['status'].toString(),
          ),
        ),
      );
    return roster;
  }

  void _reloadRoster() => setState(() => _roster = _loadRoster());

  Future<void> _save(Map<String, dynamic> roster) async {
    setState(() => _saving = true);
    try {
      final entries = asMapList(roster['entries'] ?? const [])
          .map(
            (item) => {
              'studentProfileId': item['studentProfileId'],
              'status':
                  _statuses[item['studentProfileId'].toString()] ?? 'present',
              'note': item['note'],
              'rowVersion': item['rowVersion'],
            },
          )
          .toList();
      await ref
          .read(apiClientProvider)
          .saveTeacherAttendance(
            _classId!,
            date: _date,
            session: _session,
            entries: entries,
          );
      if (mounted) {
        ScaffoldMessenger.of(
          context,
        ).showSnackBar(const SnackBar(content: Text('Đã lưu điểm danh.')));
        _reloadRoster();
      }
    } catch (error) {
      if (mounted) {
        ScaffoldMessenger.of(
          context,
        ).showSnackBar(SnackBar(content: Text(apiErrorMessage(error))));
      }
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Điểm danh lớp')),
      body: FutureBuilder<List<Map<String, dynamic>>>(
        future: _classes,
        builder: (context, classSnapshot) {
          if (classSnapshot.connectionState != ConnectionState.done) {
            return const LoadingView();
          }
          if (classSnapshot.hasError) {
            return ErrorView(
              error: classSnapshot.error!,
              onRetry: () => setState(() => _classes = _loadClasses()),
            );
          }
          final classes = classSnapshot.data ?? [];
          if (classes.isEmpty) {
            return const EmptyView(
              message: 'Bạn chưa được phân công lớp.',
              icon: Icons.class_outlined,
            );
          }
          return Column(
            children: [
              Padding(
                padding: const EdgeInsets.all(16),
                child: Row(
                  children: [
                    Expanded(
                      child: DropdownButtonFormField<String>(
                        initialValue: _classId,
                        decoration: const InputDecoration(labelText: 'Lớp'),
                        items: classes
                            .map(
                              (item) => DropdownMenuItem(
                                value: item['classId']?.toString(),
                                child: Text(
                                  item['classDisplayName'].toString(),
                                ),
                              ),
                            )
                            .toList(),
                        onChanged: (value) {
                          _classId = value;
                          _reloadRoster();
                        },
                      ),
                    ),
                    const SizedBox(width: 10),
                    Expanded(
                      child: DropdownButtonFormField<String>(
                        initialValue: _session,
                        decoration: const InputDecoration(labelText: 'Buổi'),
                        items: const [
                          DropdownMenuItem(
                            value: 'morning',
                            child: Text('Sáng'),
                          ),
                          DropdownMenuItem(
                            value: 'afternoon',
                            child: Text('Chiều'),
                          ),
                        ],
                        onChanged: (value) {
                          _session = value!;
                          _reloadRoster();
                        },
                      ),
                    ),
                  ],
                ),
              ),
              Expanded(
                child: FutureBuilder<Map<String, dynamic>>(
                  future: _roster,
                  builder: (context, snapshot) {
                    if (snapshot.connectionState != ConnectionState.done) {
                      return const LoadingView();
                    }
                    if (snapshot.hasError) {
                      return ErrorView(
                        error: snapshot.error!,
                        onRetry: _reloadRoster,
                      );
                    }
                    final roster = snapshot.data!;
                    final entries = asMapList(roster['entries'] ?? const []);
                    return Column(
                      children: [
                        Expanded(
                          child: ListView.separated(
                            padding: const EdgeInsets.symmetric(horizontal: 16),
                            itemCount: entries.length,
                            separatorBuilder: (_, _) =>
                                const SizedBox(height: 8),
                            itemBuilder: (context, index) {
                              final item = entries[index];
                              final id = item['studentProfileId'].toString();
                              return Card(
                                child: ListTile(
                                  title: Text(
                                    item['studentDisplayName'].toString(),
                                  ),
                                  subtitle: Text(
                                    item['studentCode'].toString(),
                                  ),
                                  trailing: DropdownButton<String>(
                                    value: _statuses[id] ?? 'present',
                                    items: const [
                                      DropdownMenuItem(
                                        value: 'present',
                                        child: Text('Có mặt'),
                                      ),
                                      DropdownMenuItem(
                                        value: 'late',
                                        child: Text('Đi muộn'),
                                      ),
                                      DropdownMenuItem(
                                        value: 'excusedAbsence',
                                        child: Text('Nghỉ CP'),
                                      ),
                                      DropdownMenuItem(
                                        value: 'unexcusedAbsence',
                                        child: Text('Nghỉ KP'),
                                      ),
                                    ],
                                    onChanged: (value) {
                                      setState(() => _statuses[id] = value!);
                                    },
                                  ),
                                ),
                              );
                            },
                          ),
                        ),
                        SafeArea(
                          minimum: const EdgeInsets.all(16),
                          child: SizedBox(
                            width: double.infinity,
                            child: FilledButton.icon(
                              onPressed: _saving ? null : () => _save(roster),
                              icon: const Icon(Icons.save_outlined),
                              label: Text(
                                _saving ? 'Đang lưu...' : 'Lưu điểm danh',
                              ),
                            ),
                          ),
                        ),
                      ],
                    );
                  },
                ),
              ),
            ],
          );
        },
      ),
    );
  }
}

Color _statusColor(String value) => switch (value) {
  'present' => AppTheme.success,
  'late' => AppTheme.warning,
  'excusedAbsence' => Colors.blue,
  'unexcusedAbsence' => AppTheme.error,
  _ => AppTheme.textSecondary,
};

IconData _statusIcon(String value) => switch (value) {
  'present' => Icons.check_circle_outline,
  'late' => Icons.schedule,
  'excusedAbsence' => Icons.event_busy_outlined,
  'unexcusedAbsence' => Icons.cancel_outlined,
  _ => Icons.help_outline,
};

String _statusLabel(String value) => switch (value) {
  'present' => 'Có mặt',
  'late' => 'Đi muộn',
  'excusedAbsence' => 'Nghỉ có phép',
  'unexcusedAbsence' => 'Nghỉ không phép',
  _ => 'Chưa đánh dấu',
};
