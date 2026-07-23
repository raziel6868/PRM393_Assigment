import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../core/auth.dart';
import '../../shared/api_view.dart';
import '../../shared/theme.dart';

class AnnouncementsScreen extends ConsumerStatefulWidget {
  const AnnouncementsScreen({super.key});

  @override
  ConsumerState<AnnouncementsScreen> createState() =>
      _AnnouncementsScreenState();
}

class _AnnouncementsScreenState extends ConsumerState<AnnouncementsScreen> {
  late Future<List<Map<String, dynamic>>> _announcements;

  @override
  void initState() {
    super.initState();
    _announcements = _load();
  }

  Future<List<Map<String, dynamic>>> _load() async {
    final response = await ref.read(apiClientProvider).getAnnouncements();
    return asMapList(asMap(response.data)['items'] ?? const []);
  }

  void _reload() => setState(() => _announcements = _load());

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Thông báo')),
      body: FutureBuilder<List<Map<String, dynamic>>>(
        future: _announcements,
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
              message: 'Chưa có thông báo dành cho bạn.',
              icon: Icons.notifications_none,
            );
          }
          return RefreshIndicator(
            onRefresh: () async => _reload(),
            child: ListView.separated(
              padding: const EdgeInsets.all(16),
              itemCount: items.length,
              separatorBuilder: (_, _) => const SizedBox(height: 10),
              itemBuilder: (context, index) {
                final item = items[index];
                return Card(
                  child: ListTile(
                    contentPadding: const EdgeInsets.all(16),
                    leading: const CircleAvatar(
                      backgroundColor: AppTheme.orangeTint,
                      child: Icon(
                        Icons.campaign_outlined,
                        color: AppTheme.primaryOrange,
                      ),
                    ),
                    title: Text(
                      item['title']?.toString() ?? 'Thông báo',
                      style: const TextStyle(fontWeight: FontWeight.w700),
                    ),
                    subtitle: Padding(
                      padding: const EdgeInsets.only(top: 6),
                      child: Text(
                        '${item['bodyPreview'] ?? ''}\n'
                        '${formatDateTime(item['publishedAtUtc'])}',
                        maxLines: 3,
                        overflow: TextOverflow.ellipsis,
                      ),
                    ),
                    trailing: const Icon(Icons.chevron_right),
                    onTap: () => context.push('/announcements/${item['id']}'),
                  ),
                );
              },
            ),
          );
        },
      ),
    );
  }
}
