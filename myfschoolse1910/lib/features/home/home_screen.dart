import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../core/auth.dart';
import '../../shared/theme.dart';

class HomeScreen extends ConsumerWidget {
  const HomeScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final session = ref.watch(authProvider).session;
    final services = _servicesFor(session);

    return Scaffold(
      appBar: AppBar(
        title: const Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(Icons.bolt, color: AppTheme.primaryOrange),
            SizedBox(width: 8),
            Text('MyFSchool'),
          ],
        ),
        actions: [
          IconButton(
            icon: const Icon(Icons.notifications_outlined),
            tooltip: 'Thông báo',
            onPressed: () => context.go('/announcements'),
          ),
          PopupMenuButton<String>(
            icon: const Icon(Icons.account_circle_outlined),
            onSelected: (value) async {
              if (value == 'profile') {
                context.go('/profile');
              } else if (value == 'logout') {
                await ref.read(authProvider.notifier).logout();
                if (context.mounted) context.go('/login');
              }
            },
            itemBuilder: (_) => const [
              PopupMenuItem(
                value: 'profile',
                child: ListTile(
                  contentPadding: EdgeInsets.zero,
                  leading: Icon(Icons.person_outline),
                  title: Text('Tài khoản'),
                ),
              ),
              PopupMenuItem(
                value: 'logout',
                child: ListTile(
                  contentPadding: EdgeInsets.zero,
                  leading: Icon(Icons.logout, color: AppTheme.error),
                  title: Text(
                    'Đăng xuất',
                    style: TextStyle(color: AppTheme.error),
                  ),
                ),
              ),
            ],
          ),
        ],
      ),
      body: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          Container(
            padding: const EdgeInsets.all(22),
            decoration: BoxDecoration(
              gradient: const LinearGradient(
                colors: [AppTheme.primaryOrange, AppTheme.orangeDark],
              ),
              borderRadius: BorderRadius.circular(18),
              boxShadow: [
                BoxShadow(
                  color: AppTheme.primaryOrange.withValues(alpha: .2),
                  blurRadius: 18,
                  offset: const Offset(0, 8),
                ),
              ],
            ),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  _greeting(),
                  style: const TextStyle(
                    color: Colors.white,
                    fontSize: 24,
                    fontWeight: FontWeight.w800,
                  ),
                ),
                const SizedBox(height: 6),
                Text(
                  session?.displayName ?? 'Người dùng',
                  style: const TextStyle(color: Colors.white, fontSize: 17),
                ),
                const SizedBox(height: 8),
                Text(
                  _roleLabel(session),
                  style: const TextStyle(color: Colors.white70),
                ),
              ],
            ),
          ),
          const SizedBox(height: 26),
          Text(
            'Dịch vụ trường học',
            style: Theme.of(
              context,
            ).textTheme.titleLarge?.copyWith(fontWeight: FontWeight.w800),
          ),
          const SizedBox(height: 14),
          GridView.builder(
            shrinkWrap: true,
            physics: const NeverScrollableScrollPhysics(),
            itemCount: services.length,
            gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
              crossAxisCount: 2,
              mainAxisSpacing: 12,
              crossAxisSpacing: 12,
              childAspectRatio: 1.12,
            ),
            itemBuilder: (context, index) {
              final item = services[index];
              return Card(
                child: InkWell(
                  onTap: () => context.go(item.route),
                  borderRadius: BorderRadius.circular(12),
                  child: Padding(
                    padding: const EdgeInsets.all(16),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Container(
                          padding: const EdgeInsets.all(11),
                          decoration: BoxDecoration(
                            color: item.color.withValues(alpha: .12),
                            borderRadius: BorderRadius.circular(12),
                          ),
                          child: Icon(item.icon, color: item.color),
                        ),
                        const Spacer(),
                        Text(
                          item.title,
                          style: const TextStyle(
                            fontWeight: FontWeight.w800,
                            fontSize: 16,
                          ),
                        ),
                        const SizedBox(height: 4),
                        Text(
                          item.subtitle,
                          style: const TextStyle(
                            color: AppTheme.textSecondary,
                            fontSize: 12,
                          ),
                        ),
                      ],
                    ),
                  ),
                ),
              );
            },
          ),
        ],
      ),
    );
  }

  List<_ServiceItem> _servicesFor(Session? session) {
    final common = <_ServiceItem>[
      const _ServiceItem(
        'Lịch học',
        'Thời khóa biểu tuần',
        '/schedule',
        Icons.calendar_month_outlined,
        Colors.blue,
      ),
      const _ServiceItem(
        'Sự kiện',
        'Hoạt động nhà trường',
        '/events',
        Icons.event_outlined,
        Colors.purple,
      ),
      const _ServiceItem(
        'Thông báo',
        'Thông tin mới nhất',
        '/announcements',
        Icons.campaign_outlined,
        AppTheme.primaryOrange,
      ),
      const _ServiceItem(
        'Câu lạc bộ',
        'Khám phá cộng đồng',
        '/clubs',
        Icons.groups_outlined,
        Colors.green,
      ),
    ];
    if (session?.isTeacher == true) {
      return [
        const _ServiceItem(
          'Điểm danh',
          'Quản lý lớp hôm nay',
          '/teacher/attendance',
          Icons.fact_check_outlined,
          Colors.teal,
        ),
        const _ServiceItem(
          'Duyệt đơn',
          'Đơn xin nghỉ',
          '/teacher/leave-requests',
          Icons.approval_outlined,
          Colors.amber,
        ),
        ...common,
      ];
    }
    return [
      const _ServiceItem(
        'Điểm số',
        'Kết quả học tập',
        '/grades',
        Icons.school_outlined,
        AppTheme.primaryOrange,
      ),
      const _ServiceItem(
        'Điểm danh',
        'Lịch sử chuyên cần',
        '/attendance',
        Icons.fact_check_outlined,
        Colors.teal,
      ),
      const _ServiceItem(
        'Đơn từ',
        'Xin nghỉ và trạng thái',
        '/leave-requests',
        Icons.description_outlined,
        Colors.amber,
      ),
      ...common,
    ];
  }

  String _greeting() {
    final hour = DateTime.now().hour;
    if (hour < 12) return 'Chào buổi sáng!';
    if (hour < 18) return 'Chào buổi chiều!';
    return 'Chào buổi tối!';
  }

  String _roleLabel(Session? session) {
    if (session?.isTeacher == true) return 'Giáo viên';
    if (session?.isParent == true) return 'Phụ huynh / Người giám hộ';
    return 'Học sinh';
  }
}

class _ServiceItem {
  final String title;
  final String subtitle;
  final String route;
  final IconData icon;
  final Color color;

  const _ServiceItem(
    this.title,
    this.subtitle,
    this.route,
    this.icon,
    this.color,
  );
}
