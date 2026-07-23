import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/auth.dart';
import '../../shared/api_view.dart';
import '../../shared/theme.dart';

class EventDetailScreen extends ConsumerStatefulWidget {
  final String eventId;

  const EventDetailScreen({super.key, required this.eventId});

  @override
  ConsumerState<EventDetailScreen> createState() => _EventDetailScreenState();
}

class _EventDetailScreenState extends ConsumerState<EventDetailScreen> {
  late Future<Map<String, dynamic>> _event;

  @override
  void initState() {
    super.initState();
    _event = _load();
  }

  Future<Map<String, dynamic>> _load() async {
    final response = await ref
        .read(apiClientProvider)
        .getEventDetail(widget.eventId);
    return asMap(response.data);
  }

  void _reload() => setState(() => _event = _load());

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Chi tiết sự kiện')),
      body: FutureBuilder<Map<String, dynamic>>(
        future: _event,
        builder: (context, snapshot) {
          if (snapshot.connectionState != ConnectionState.done) {
            return const LoadingView();
          }
          if (snapshot.hasError) {
            return ErrorView(error: snapshot.error!, onRetry: _reload);
          }
          final event = snapshot.data!;
          return ListView(
            padding: const EdgeInsets.all(16),
            children: [
              Container(
                height: 150,
                decoration: BoxDecoration(
                  gradient: const LinearGradient(
                    colors: [AppTheme.primaryOrange, AppTheme.orangeDark],
                  ),
                  borderRadius: BorderRadius.circular(16),
                ),
                child: const Icon(
                  Icons.celebration_outlined,
                  color: Colors.white,
                  size: 72,
                ),
              ),
              const SizedBox(height: 18),
              Text(
                event['title']?.toString() ?? 'Sự kiện',
                style: Theme.of(context).textTheme.headlineSmall?.copyWith(
                  fontWeight: FontWeight.w800,
                ),
              ),
              const SizedBox(height: 16),
              _InfoRow(
                icon: Icons.schedule_outlined,
                label: 'Bắt đầu',
                value: formatDateTime(event['startAtUtc']),
              ),
              _InfoRow(
                icon: Icons.event_available_outlined,
                label: 'Kết thúc',
                value: formatDateTime(event['endAtUtc']),
              ),
              _InfoRow(
                icon: Icons.location_on_outlined,
                label: 'Địa điểm',
                value: event['location']?.toString() ?? 'Chưa cập nhật',
              ),
              _InfoRow(
                icon: Icons.groups_outlined,
                label: 'Đối tượng',
                value: event['audience']?.toString() ?? 'Toàn trường',
              ),
              if ((event['description']?.toString() ?? '').isNotEmpty) ...[
                const SizedBox(height: 16),
                Text(
                  'Nội dung',
                  style: Theme.of(context).textTheme.titleMedium,
                ),
                const SizedBox(height: 8),
                Text(event['description'].toString()),
              ],
            ],
          );
        },
      ),
    );
  }
}

class _InfoRow extends StatelessWidget {
  final IconData icon;
  final String label;
  final String value;

  const _InfoRow({
    required this.icon,
    required this.label,
    required this.value,
  });

  @override
  Widget build(BuildContext context) {
    return Card(
      child: ListTile(
        leading: Icon(icon, color: AppTheme.primaryOrange),
        title: Text(label),
        subtitle: Text(value),
      ),
    );
  }
}
