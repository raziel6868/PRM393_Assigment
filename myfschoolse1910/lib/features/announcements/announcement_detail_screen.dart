import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/auth.dart';
import '../../shared/api_view.dart';
import '../../shared/theme.dart';

class AnnouncementDetailScreen extends ConsumerStatefulWidget {
  final String announcementId;

  const AnnouncementDetailScreen({super.key, required this.announcementId});

  @override
  ConsumerState<AnnouncementDetailScreen> createState() =>
      _AnnouncementDetailScreenState();
}

class _AnnouncementDetailScreenState
    extends ConsumerState<AnnouncementDetailScreen> {
  late Future<Map<String, dynamic>> _announcement;

  @override
  void initState() {
    super.initState();
    _announcement = _load();
  }

  Future<Map<String, dynamic>> _load() async {
    final client = ref.read(apiClientProvider);
    final response = await client.getAnnouncementDetail(widget.announcementId);
    try {
      await client.markAnnouncementAsRead(widget.announcementId);
    } catch (error) {
      debugPrint('Announcement read marker failed: $error');
    }
    return asMap(response.data);
  }

  void _reload() => setState(() => _announcement = _load());

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Chi tiết thông báo')),
      body: FutureBuilder<Map<String, dynamic>>(
        future: _announcement,
        builder: (context, snapshot) {
          if (snapshot.connectionState != ConnectionState.done) {
            return const LoadingView();
          }
          if (snapshot.hasError) {
            return ErrorView(error: snapshot.error!, onRetry: _reload);
          }
          final item = snapshot.data!;
          return ListView(
            padding: const EdgeInsets.all(18),
            children: [
              const Icon(
                Icons.campaign_outlined,
                size: 54,
                color: AppTheme.primaryOrange,
              ),
              const SizedBox(height: 16),
              Text(
                item['title']?.toString() ?? 'Thông báo',
                style: Theme.of(context).textTheme.headlineSmall?.copyWith(
                  fontWeight: FontWeight.w800,
                ),
              ),
              const SizedBox(height: 8),
              Text(
                '${item['authorDisplayName'] ?? 'Nhà trường'} · '
                '${formatDateTime(item['publishedAtUtc'])}',
                style: const TextStyle(color: AppTheme.textSecondary),
              ),
              const Divider(height: 32),
              SelectableText(
                item['body']?.toString() ?? '',
                style: const TextStyle(fontSize: 16, height: 1.55),
              ),
            ],
          );
        },
      ),
    );
  }
}
