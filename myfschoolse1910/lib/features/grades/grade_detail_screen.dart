import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/auth.dart';
import '../../shared/api_view.dart';
import '../../shared/theme.dart';

class GradeDetailScreen extends ConsumerStatefulWidget {
  final String gradeId;

  const GradeDetailScreen({super.key, required this.gradeId});

  @override
  ConsumerState<GradeDetailScreen> createState() => _GradeDetailScreenState();
}

class _GradeDetailScreenState extends ConsumerState<GradeDetailScreen> {
  late Future<Map<String, dynamic>> _detail;

  @override
  void initState() {
    super.initState();
    _detail = _load();
  }

  Future<Map<String, dynamic>> _load() async {
    final response = await ref
        .read(apiClientProvider)
        .getGradeDetail(widget.gradeId);
    return asMap(response.data);
  }

  void _reload() => setState(() => _detail = _load());

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Chi tiết điểm')),
      body: FutureBuilder<Map<String, dynamic>>(
        future: _detail,
        builder: (context, snapshot) {
          if (snapshot.connectionState != ConnectionState.done) {
            return const LoadingView();
          }
          if (snapshot.hasError) {
            return ErrorView(error: snapshot.error!, onRetry: _reload);
          }
          final grade = snapshot.data!;
          return ListView(
            padding: const EdgeInsets.all(16),
            children: [
              Card(
                child: Padding(
                  padding: const EdgeInsets.all(24),
                  child: Column(
                    children: [
                      Text(
                        grade['score']?.toString() ?? '—',
                        style: const TextStyle(
                          fontSize: 48,
                          color: AppTheme.primaryOrange,
                          fontWeight: FontWeight.w800,
                        ),
                      ),
                      Text(
                        'trên ${grade['maxScore'] ?? 10}',
                        style: const TextStyle(color: AppTheme.textSecondary),
                      ),
                      const SizedBox(height: 12),
                      Text(
                        grade['subjectName']?.toString() ?? 'Môn học',
                        style: Theme.of(context).textTheme.titleLarge,
                      ),
                    ],
                  ),
                ),
              ),
              const SizedBox(height: 16),
              Card(
                child: Column(
                  children: [
                    ListTile(
                      leading: const Icon(Icons.assignment_outlined),
                      title: const Text('Bài đánh giá'),
                      subtitle: Text(
                        grade['assessmentName']?.toString() ?? '—',
                      ),
                    ),
                    ListTile(
                      leading: const Icon(Icons.class_outlined),
                      title: const Text('Lớp'),
                      subtitle: Text(grade['classCode']?.toString() ?? '—'),
                    ),
                    ListTile(
                      leading: const Icon(Icons.calendar_month_outlined),
                      title: const Text('Học kỳ'),
                      subtitle: Text(
                        'Học kỳ ${grade['semester']} · '
                        '${grade['schoolYearCode']}',
                      ),
                    ),
                    ListTile(
                      leading: const Icon(Icons.schedule_outlined),
                      title: const Text('Ghi nhận lúc'),
                      subtitle: Text(formatDateTime(grade['recordedAtUtc'])),
                    ),
                  ],
                ),
              ),
              if ((grade['teacherComment']?.toString() ?? '').isNotEmpty) ...[
                const SizedBox(height: 16),
                Card(
                  color: AppTheme.orangeTint,
                  child: Padding(
                    padding: const EdgeInsets.all(16),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        const Text(
                          'Nhận xét của giáo viên',
                          style: TextStyle(fontWeight: FontWeight.w700),
                        ),
                        const SizedBox(height: 8),
                        Text(grade['teacherComment'].toString()),
                      ],
                    ),
                  ),
                ),
              ],
            ],
          );
        },
      ),
    );
  }
}
