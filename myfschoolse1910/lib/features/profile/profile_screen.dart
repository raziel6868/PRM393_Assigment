import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/auth.dart';
import '../../shared/theme.dart';

class ProfileScreen extends ConsumerWidget {
  const ProfileScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final session = ref.watch(authProvider).session;
    final roles =
        session?.roles
            .map(
              (role) => switch (role) {
                UserRole.teacher => 'Giáo viên',
                UserRole.parent => 'Phụ huynh / Người giám hộ',
                UserRole.student => 'Học sinh',
                UserRole.administrator => 'Quản trị viên',
              },
            )
            .join(', ') ??
        '—';
    final name = session?.displayName ?? 'Người dùng';
    return Scaffold(
      appBar: AppBar(title: const Text('Tài khoản')),
      body: ListView(
        padding: const EdgeInsets.all(20),
        children: [
          CircleAvatar(
            radius: 42,
            backgroundColor: AppTheme.primaryOrange,
            child: Text(
              name.isEmpty ? 'M' : name[0].toUpperCase(),
              style: const TextStyle(
                color: Colors.white,
                fontSize: 32,
                fontWeight: FontWeight.w800,
              ),
            ),
          ),
          const SizedBox(height: 16),
          Text(
            name,
            textAlign: TextAlign.center,
            style: Theme.of(
              context,
            ).textTheme.headlineSmall?.copyWith(fontWeight: FontWeight.w800),
          ),
          const SizedBox(height: 24),
          Card(
            child: ListTile(
              leading: const Icon(
                Icons.badge_outlined,
                color: AppTheme.primaryOrange,
              ),
              title: const Text('Vai trò'),
              subtitle: Text(roles),
            ),
          ),
          Card(
            child: ListTile(
              leading: const Icon(
                Icons.security_outlined,
                color: AppTheme.primaryOrange,
              ),
              title: const Text('Tài khoản do nhà trường cấp'),
              subtitle: const Text(
                'Liên hệ nhà trường khi cần hỗ trợ thông tin hoặc mật khẩu.',
              ),
            ),
          ),
        ],
      ),
    );
  }
}
