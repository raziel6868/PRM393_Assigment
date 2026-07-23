import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/auth.dart';
import '../../shared/api_view.dart';
import '../../shared/theme.dart';

class ClubDetailScreen extends ConsumerStatefulWidget {
  final String clubId;

  const ClubDetailScreen({super.key, required this.clubId});

  @override
  ConsumerState<ClubDetailScreen> createState() => _ClubDetailScreenState();
}

class _ClubDetailScreenState extends ConsumerState<ClubDetailScreen> {
  late Future<Map<String, dynamic>> _club;
  bool _submitting = false;

  @override
  void initState() {
    super.initState();
    _club = _load();
  }

  Future<Map<String, dynamic>> _load() async {
    final response = await ref
        .read(apiClientProvider)
        .getClubDetail(widget.clubId);
    return asMap(response.data);
  }

  void _reload() => setState(() => _club = _load());

  Future<void> _changeMembership(String status) async {
    setState(() => _submitting = true);
    try {
      if (status.toLowerCase() == 'active') {
        await ref.read(apiClientProvider).leaveClub(widget.clubId);
      } else {
        await ref.read(apiClientProvider).joinClub(widget.clubId);
      }
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(
              status.toLowerCase() == 'active'
                  ? 'Đã rời câu lạc bộ.'
                  : 'Đã gửi yêu cầu tham gia.',
            ),
          ),
        );
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
    return Scaffold(
      appBar: AppBar(title: const Text('Chi tiết CLB')),
      body: FutureBuilder<Map<String, dynamic>>(
        future: _club,
        builder: (context, snapshot) {
          if (snapshot.connectionState != ConnectionState.done) {
            return const LoadingView();
          }
          if (snapshot.hasError) {
            return ErrorView(error: snapshot.error!, onRetry: _reload);
          }
          final club = snapshot.data!;
          final status = club['membershipStatus']?.toString() ?? 'none';
          final isStudent = ref.read(authProvider).session?.isStudent == true;
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
                  Icons.groups_outlined,
                  color: Colors.white,
                  size: 72,
                ),
              ),
              const SizedBox(height: 18),
              Text(
                club['displayName']?.toString() ?? 'Câu lạc bộ',
                style: Theme.of(context).textTheme.headlineSmall?.copyWith(
                  fontWeight: FontWeight.w800,
                ),
              ),
              const SizedBox(height: 8),
              Wrap(
                spacing: 8,
                children: [
                  Chip(label: Text(club['category']?.toString() ?? 'Khác')),
                  Chip(
                    label: Text(
                      '${club['currentMemberCount']}'
                      '${club['maxMembers'] == null ? '' : '/${club['maxMembers']}'} '
                      'thành viên',
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 16),
              Text(
                club['description']?.toString() ??
                    'Câu lạc bộ chưa cập nhật giới thiệu.',
              ),
              if (isStudent) ...[
                const SizedBox(height: 24),
                if (status.toLowerCase() == 'pending')
                  const FilledButton(
                    onPressed: null,
                    child: Text('Đang chờ duyệt'),
                  )
                else
                  FilledButton.icon(
                    onPressed: _submitting
                        ? null
                        : () => _changeMembership(status),
                    icon: Icon(
                      status.toLowerCase() == 'active'
                          ? Icons.logout
                          : Icons.group_add_outlined,
                    ),
                    label: Text(
                      status.toLowerCase() == 'active'
                          ? 'Rời câu lạc bộ'
                          : 'Gửi yêu cầu tham gia',
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
