import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/auth.dart';
import '../../shared/api_view.dart';
import '../../shared/theme.dart';

class ScheduleScreen extends ConsumerStatefulWidget {
  const ScheduleScreen({super.key});

  @override
  ConsumerState<ScheduleScreen> createState() => _ScheduleScreenState();
}

class _ScheduleScreenState extends ConsumerState<ScheduleScreen> {
  late DateTime _weekStart;
  late Future<List<Map<String, dynamic>>> _entries;
  List<Map<String, dynamic>> _children = [];
  String? _studentProfileId;

  @override
  void initState() {
    super.initState();
    final today = DateTime.now();
    _weekStart = DateTime(
      today.year,
      today.month,
      today.day,
    ).subtract(Duration(days: today.weekday - 1));
    _entries = _load();
  }

  Future<List<Map<String, dynamic>>> _load() async {
    final session = ref.read(authProvider).session;
    if (session?.isParent == true && _children.isEmpty) {
      final scopeResponse = await ref.read(apiClientProvider).getClasses();
      final seen = <String>{};
      _children = asMapList(scopeResponse.data)
          .where((item) => seen.add(item['studentProfileId']?.toString() ?? ''))
          .toList();
      _studentProfileId ??= _children.isEmpty
          ? null
          : _children.first['studentProfileId']?.toString();
    }
    final response = await ref
        .read(apiClientProvider)
        .getWeeklyTimetable(_weekStart, studentProfileId: _studentProfileId);
    return asMapList(response.data);
  }

  void _reload() => setState(() => _entries = _load());

  void _moveWeek(int offset) {
    setState(() {
      _weekStart = _weekStart.add(Duration(days: offset * 7));
      _entries = _load();
    });
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Lịch học'),
        actions: [
          IconButton(
            onPressed: _reload,
            icon: const Icon(Icons.refresh),
            tooltip: 'Tải lại',
          ),
        ],
      ),
      body: Column(
        children: [
          if (_children.length > 1)
            Padding(
              padding: const EdgeInsets.fromLTRB(16, 12, 16, 0),
              child: DropdownButtonFormField<String>(
                initialValue: _studentProfileId,
                decoration: const InputDecoration(
                  labelText: 'Học sinh',
                  prefixIcon: Icon(Icons.child_care_outlined),
                ),
                items: _children
                    .map(
                      (child) => DropdownMenuItem(
                        value: child['studentProfileId']?.toString(),
                        child: Text(
                          '${child['studentDisplayName']} · '
                          '${child['classDisplayName']}',
                        ),
                      ),
                    )
                    .toList(),
                onChanged: (value) {
                  setState(() {
                    _studentProfileId = value;
                    _entries = _load();
                  });
                },
              ),
            ),
          Padding(
            padding: const EdgeInsets.all(16),
            child: Row(
              children: [
                IconButton(
                  onPressed: () => _moveWeek(-1),
                  icon: const Icon(Icons.chevron_left),
                ),
                Expanded(
                  child: Text(
                    '${formatDate(_weekStart.toIso8601String())} – '
                    '${formatDate(_weekStart.add(const Duration(days: 6)).toIso8601String())}',
                    textAlign: TextAlign.center,
                    style: const TextStyle(fontWeight: FontWeight.w700),
                  ),
                ),
                IconButton(
                  onPressed: () => _moveWeek(1),
                  icon: const Icon(Icons.chevron_right),
                ),
              ],
            ),
          ),
          Expanded(
            child: FutureBuilder<List<Map<String, dynamic>>>(
              future: _entries,
              builder: (context, snapshot) {
                if (snapshot.connectionState != ConnectionState.done) {
                  return const LoadingView();
                }
                if (snapshot.hasError) {
                  return ErrorView(error: snapshot.error!, onRetry: _reload);
                }
                final entries = snapshot.data ?? [];
                if (entries.isEmpty) {
                  return const EmptyView(
                    message: 'Không có tiết học trong tuần này.',
                    icon: Icons.event_available_outlined,
                  );
                }
                final grouped = <int, List<Map<String, dynamic>>>{};
                for (final entry in entries) {
                  final day = entry['dayOfWeek'] as int? ?? 1;
                  grouped.putIfAbsent(day, () => []).add(entry);
                }
                final days = grouped.keys.toList()..sort();
                return RefreshIndicator(
                  onRefresh: () async => _reload(),
                  child: ListView.builder(
                    padding: const EdgeInsets.fromLTRB(16, 0, 16, 24),
                    itemCount: days.length,
                    itemBuilder: (context, index) {
                      final day = days[index];
                      final dayEntries = grouped[day]!
                        ..sort(
                          (a, b) => (a['startTime']?.toString() ?? '')
                              .compareTo(b['startTime']?.toString() ?? ''),
                        );
                      return Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Padding(
                            padding: const EdgeInsets.symmetric(vertical: 10),
                            child: Text(
                              _weekdayLabel(day),
                              style: Theme.of(context).textTheme.titleMedium,
                            ),
                          ),
                          ...dayEntries.map(_ScheduleCard.new),
                        ],
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

  String _weekdayLabel(int value) =>
      value == 7 ? 'Chủ nhật' : 'Thứ ${value + 1}';
}

class _ScheduleCard extends StatelessWidget {
  final Map<String, dynamic> entry;

  const _ScheduleCard(this.entry);

  @override
  Widget build(BuildContext context) {
    return Card(
      margin: const EdgeInsets.only(bottom: 10),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Container(
              width: 4,
              height: 68,
              decoration: BoxDecoration(
                color: AppTheme.primaryOrange,
                borderRadius: BorderRadius.circular(4),
              ),
            ),
            const SizedBox(width: 14),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    entry['subjectDisplayName']?.toString() ?? 'Môn học',
                    style: const TextStyle(
                      fontSize: 16,
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                  const SizedBox(height: 6),
                  Text(
                    '${entry['startTime']} – ${entry['endTime']} · '
                    '${entry['classDisplayName']}',
                  ),
                  const SizedBox(height: 4),
                  Text(
                    '${entry['room'] ?? 'Chưa xếp phòng'} · '
                    '${entry['teacherDisplayName']}',
                    style: const TextStyle(color: AppTheme.textSecondary),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}
