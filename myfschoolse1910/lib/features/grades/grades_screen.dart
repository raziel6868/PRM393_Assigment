import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../core/auth.dart';
import '../../shared/api_view.dart';
import '../../shared/theme.dart';

class GradesScreen extends ConsumerStatefulWidget {
  const GradesScreen({super.key});

  @override
  ConsumerState<GradesScreen> createState() => _GradesScreenState();
}

class _GradesScreenState extends ConsumerState<GradesScreen> {
  late Future<List<Map<String, dynamic>>> _semesters;
  String? _selectedSemesterId;
  Future<Map<String, dynamic>>? _summary;

  @override
  void initState() {
    super.initState();
    _semesters = _loadSemesters();
  }

  Future<List<Map<String, dynamic>>> _loadSemesters() async {
    final response = await ref.read(apiClientProvider).getSemesters();
    final semesters = asMapList(response.data);
    if (semesters.isNotEmpty) {
      _selectedSemesterId ??= semesters.first['id']?.toString();
      _summary = _loadSummary(_selectedSemesterId!);
    }
    return semesters;
  }

  Future<Map<String, dynamic>> _loadSummary(String semesterId) async {
    final response = await ref
        .read(apiClientProvider)
        .getGradeSummary(semesterId);
    return asMap(response.data);
  }

  void _selectSemester(String semesterId) {
    setState(() {
      _selectedSemesterId = semesterId;
      _summary = _loadSummary(semesterId);
    });
  }

  void _reload() {
    setState(() {
      _selectedSemesterId = null;
      _summary = null;
      _semesters = _loadSemesters();
    });
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Kết quả học tập')),
      body: FutureBuilder<List<Map<String, dynamic>>>(
        future: _semesters,
        builder: (context, semesterSnapshot) {
          if (semesterSnapshot.connectionState != ConnectionState.done) {
            return const LoadingView();
          }
          if (semesterSnapshot.hasError) {
            return ErrorView(error: semesterSnapshot.error!, onRetry: _reload);
          }
          final semesters = semesterSnapshot.data ?? [];
          if (semesters.isEmpty) {
            return const EmptyView(
              message: 'Chưa có kết quả học tập được công bố.',
              icon: Icons.school_outlined,
            );
          }
          return Column(
            children: [
              Padding(
                padding: const EdgeInsets.all(16),
                child: DropdownButtonFormField<String>(
                  initialValue: _selectedSemesterId,
                  decoration: const InputDecoration(
                    labelText: 'Học kỳ',
                    prefixIcon: Icon(Icons.calendar_month_outlined),
                  ),
                  items: semesters
                      .map(
                        (item) => DropdownMenuItem(
                          value: item['id']?.toString(),
                          child: Text(
                            'Học kỳ ${item['semesterNumber']} · '
                            '${item['schoolYearCode']}',
                          ),
                        ),
                      )
                      .toList(),
                  onChanged: (value) {
                    if (value != null) _selectSemester(value);
                  },
                ),
              ),
              Expanded(
                child: FutureBuilder<Map<String, dynamic>>(
                  future: _summary,
                  builder: (context, snapshot) {
                    if (snapshot.connectionState != ConnectionState.done) {
                      return const LoadingView();
                    }
                    if (snapshot.hasError) {
                      return ErrorView(
                        error: snapshot.error!,
                        onRetry: () => _selectSemester(_selectedSemesterId!),
                      );
                    }
                    final subjects = asMapList(
                      snapshot.data?['subjects'] ?? const [],
                    );
                    if (subjects.isEmpty) {
                      return const EmptyView(
                        message: 'Học kỳ này chưa có điểm.',
                        icon: Icons.grade_outlined,
                      );
                    }
                    return RefreshIndicator(
                      onRefresh: () async =>
                          _selectSemester(_selectedSemesterId!),
                      child: ListView.separated(
                        padding: const EdgeInsets.fromLTRB(16, 0, 16, 24),
                        itemCount: subjects.length,
                        separatorBuilder: (_, _) => const SizedBox(height: 12),
                        itemBuilder: (context, index) {
                          final subject = subjects[index];
                          final grades = asMapList(
                            subject['grades'] ?? const [],
                          );
                          return Card(
                            child: ExpansionTile(
                              leading: const CircleAvatar(
                                backgroundColor: AppTheme.orangeTint,
                                child: Icon(
                                  Icons.menu_book_outlined,
                                  color: AppTheme.primaryOrange,
                                ),
                              ),
                              title: Text(
                                subject['subjectName']?.toString() ?? 'Môn học',
                                style: const TextStyle(
                                  fontWeight: FontWeight.w700,
                                ),
                              ),
                              subtitle: Text(
                                '${subject['gradeCount'] ?? 0} đầu điểm',
                              ),
                              trailing: _ScoreBadge(
                                score: subject['averageScore'],
                              ),
                              children: grades
                                  .map(
                                    (grade) => ListTile(
                                      title: Text(
                                        grade['assessmentName']?.toString() ??
                                            'Bài đánh giá',
                                      ),
                                      trailing: _ScoreBadge(
                                        score: grade['score'],
                                      ),
                                      onTap: () => context.push(
                                        '/grades/${grade['gradeId']}',
                                      ),
                                    ),
                                  )
                                  .toList(),
                            ),
                          );
                        },
                      ),
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

class _ScoreBadge extends StatelessWidget {
  final dynamic score;

  const _ScoreBadge({required this.score});

  @override
  Widget build(BuildContext context) {
    return Container(
      constraints: const BoxConstraints(minWidth: 48),
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 8),
      decoration: BoxDecoration(
        color: AppTheme.orangeTint,
        borderRadius: BorderRadius.circular(12),
      ),
      child: Text(
        score == null ? '—' : score.toString(),
        textAlign: TextAlign.center,
        style: const TextStyle(
          color: AppTheme.primaryOrange,
          fontWeight: FontWeight.w800,
        ),
      ),
    );
  }
}
