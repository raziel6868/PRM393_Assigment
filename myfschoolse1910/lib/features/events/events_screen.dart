import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../core/auth.dart';
import '../../shared/api_view.dart';
import '../../shared/theme.dart';

class EventsScreen extends ConsumerStatefulWidget {
  const EventsScreen({super.key});

  @override
  ConsumerState<EventsScreen> createState() => _EventsScreenState();
}

class _EventsScreenState extends ConsumerState<EventsScreen> {
  late Future<List<Map<String, dynamic>>> _events;

  @override
  void initState() {
    super.initState();
    _events = _load();
  }

  Future<List<Map<String, dynamic>>> _load() async {
    final response = await ref.read(apiClientProvider).getEvents();
    return asMapList(asMap(response.data)['items'] ?? const []);
  }

  void _reload() => setState(() => _events = _load());

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Sự kiện')),
      body: FutureBuilder<List<Map<String, dynamic>>>(
        future: _events,
        builder: (context, snapshot) {
          if (snapshot.connectionState != ConnectionState.done) {
            return const LoadingView();
          }
          if (snapshot.hasError) {
            return ErrorView(error: snapshot.error!, onRetry: _reload);
          }
          final events = snapshot.data ?? [];
          if (events.isEmpty) {
            return const EmptyView(
              message: 'Chưa có sự kiện sắp tới.',
              icon: Icons.event_outlined,
            );
          }
          return RefreshIndicator(
            onRefresh: () async => _reload(),
            child: ListView.separated(
              padding: const EdgeInsets.all(16),
              itemCount: events.length,
              separatorBuilder: (_, _) => const SizedBox(height: 12),
              itemBuilder: (context, index) {
                final event = events[index];
                return Card(
                  child: InkWell(
                    onTap: () => context.push('/events/${event['id']}'),
                    borderRadius: BorderRadius.circular(12),
                    child: Padding(
                      padding: const EdgeInsets.all(16),
                      child: Row(
                        children: [
                          Container(
                            width: 58,
                            height: 68,
                            decoration: BoxDecoration(
                              color: AppTheme.orangeTint,
                              borderRadius: BorderRadius.circular(12),
                            ),
                            child: Column(
                              mainAxisAlignment: MainAxisAlignment.center,
                              children: [
                                Text(
                                  _day(event['startAtUtc']),
                                  style: const TextStyle(
                                    color: AppTheme.primaryOrange,
                                    fontSize: 22,
                                    fontWeight: FontWeight.w800,
                                  ),
                                ),
                                Text(
                                  _month(event['startAtUtc']),
                                  style: const TextStyle(
                                    color: AppTheme.primaryOrange,
                                  ),
                                ),
                              ],
                            ),
                          ),
                          const SizedBox(width: 14),
                          Expanded(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text(
                                  event['title']?.toString() ?? 'Sự kiện',
                                  style: const TextStyle(
                                    fontSize: 16,
                                    fontWeight: FontWeight.w700,
                                  ),
                                ),
                                const SizedBox(height: 6),
                                Text(formatDateTime(event['startAtUtc'])),
                                const SizedBox(height: 4),
                                Text(
                                  event['location']?.toString() ??
                                      'Chưa cập nhật địa điểm',
                                  style: const TextStyle(
                                    color: AppTheme.textSecondary,
                                  ),
                                ),
                              ],
                            ),
                          ),
                          const Icon(Icons.chevron_right),
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

  String _day(dynamic value) =>
      DateTime.tryParse(
        value?.toString() ?? '',
      )?.toLocal().day.toString().padLeft(2, '0') ??
      '—';

  String _month(dynamic value) {
    final month = DateTime.tryParse(value?.toString() ?? '')?.toLocal().month;
    return month == null ? '' : 'Tháng $month';
  }
}
