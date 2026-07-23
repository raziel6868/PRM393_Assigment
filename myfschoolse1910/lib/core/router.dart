import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../core/auth.dart';
import '../features/auth/login_screen.dart';
import '../features/auth/password_help_screen.dart';
import '../features/auth/change_temporary_password_screen.dart';
import '../features/home/home_screen.dart';
import '../features/grades/grades_screen.dart';
import '../features/grades/grade_detail_screen.dart';
import '../features/schedule/schedule_screen.dart';
import '../features/attendance/attendance_screen.dart';
import '../features/events/events_screen.dart';
import '../features/events/event_detail_screen.dart';
import '../features/leave_requests/leave_requests_screen.dart';
import '../features/leave_requests/create_leave_request_screen.dart';
import '../features/leave_requests/leave_request_detail_screen.dart';
import '../features/clubs/clubs_screen.dart';
import '../features/clubs/club_detail_screen.dart';
import '../features/announcements/announcements_screen.dart';
import '../features/announcements/announcement_detail_screen.dart';
import '../features/profile/profile_screen.dart';

final _rootNavigatorKey = GlobalKey<NavigatorState>();
final _shellNavigatorKey = GlobalKey<NavigatorState>();

final routerProvider = Provider<GoRouter>((ref) {
  final authState = ref.watch(authProvider);

  return GoRouter(
    navigatorKey: _rootNavigatorKey,
    initialLocation: '/login',
    redirect: (context, state) {
      final isAuthenticated = authState.isAuthenticated;
      final isLoggingIn =
          state.matchedLocation == '/login' ||
          state.matchedLocation == '/password-help';
      final needsPasswordChange =
          authState.session?.passwordChangeRequired ?? false;

      if (!isAuthenticated && !isLoggingIn) return '/login';
      if (isAuthenticated && isLoggingIn) return '/home';
      if (needsPasswordChange &&
          state.matchedLocation != '/change-temporary-password') {
        return '/change-temporary-password';
      }
      return null;
    },
    routes: [
      GoRoute(path: '/login', builder: (context, state) => const LoginScreen()),
      GoRoute(
        path: '/password-help',
        builder: (context, state) => const PasswordHelpScreen(),
      ),
      GoRoute(
        path: '/change-temporary-password',
        builder: (context, state) => const ChangeTemporaryPasswordScreen(),
      ),
      ShellRoute(
        navigatorKey: _shellNavigatorKey,
        builder: (context, state, child) => MainShell(child: child),
        routes: [
          GoRoute(
            path: '/home',
            builder: (context, state) => const HomeScreen(),
          ),
          GoRoute(
            path: '/attendance',
            builder: (context, state) => const AttendanceScreen(),
          ),
          GoRoute(
            path: '/teacher/attendance',
            builder: (context, state) => const AttendanceScreen(),
          ),
          GoRoute(
            path: '/grades',
            builder: (context, state) => const GradesScreen(),
            routes: [
              GoRoute(
                path: ':gradeId',
                builder: (context, state) => GradeDetailScreen(
                  gradeId: state.pathParameters['gradeId']!,
                ),
              ),
            ],
          ),
          GoRoute(
            path: '/schedule',
            builder: (context, state) => const ScheduleScreen(),
          ),
          GoRoute(
            path: '/events',
            builder: (context, state) => const EventsScreen(),
            routes: [
              GoRoute(
                path: ':eventId',
                builder: (context, state) => EventDetailScreen(
                  eventId: state.pathParameters['eventId']!,
                ),
              ),
            ],
          ),
          GoRoute(
            path: '/leave-requests',
            builder: (context, state) => const LeaveRequestsScreen(),
            routes: [
              GoRoute(
                path: 'new',
                builder: (context, state) => const CreateLeaveRequestScreen(),
              ),
              GoRoute(
                path: ':requestId',
                builder: (context, state) => LeaveRequestDetailScreen(
                  requestId: state.pathParameters['requestId']!,
                  initial: state.extra is Map<String, dynamic>
                      ? state.extra! as Map<String, dynamic>
                      : null,
                ),
              ),
            ],
          ),
          GoRoute(
            path: '/clubs',
            builder: (context, state) => const ClubsScreen(),
            routes: [
              GoRoute(
                path: ':clubId',
                builder: (context, state) =>
                    ClubDetailScreen(clubId: state.pathParameters['clubId']!),
              ),
            ],
          ),
          GoRoute(
            path: '/teacher/leave-requests',
            builder: (context, state) => const LeaveRequestsScreen(),
            routes: [
              GoRoute(
                path: ':requestId',
                builder: (context, state) => LeaveRequestDetailScreen(
                  requestId: state.pathParameters['requestId']!,
                  initial: state.extra is Map<String, dynamic>
                      ? state.extra! as Map<String, dynamic>
                      : null,
                ),
              ),
            ],
          ),
          GoRoute(
            path: '/announcements',
            builder: (context, state) => const AnnouncementsScreen(),
            routes: [
              GoRoute(
                path: ':announcementId',
                builder: (context, state) => AnnouncementDetailScreen(
                  announcementId: state.pathParameters['announcementId']!,
                ),
              ),
            ],
          ),
          GoRoute(
            path: '/profile',
            builder: (context, state) => const ProfileScreen(),
          ),
        ],
      ),
    ],
  );
});

class MainShell extends StatelessWidget {
  final Widget child;
  const MainShell({super.key, required this.child});

  @override
  Widget build(BuildContext context) {
    return Scaffold(body: child, bottomNavigationBar: const AppBottomNavBar());
  }
}

class AppBottomNavBar extends StatelessWidget {
  const AppBottomNavBar({super.key});

  @override
  Widget build(BuildContext context) {
    final location = GoRouterState.of(context).matchedLocation;

    int currentIndex = 0;
    if (location.startsWith('/schedule'))
      currentIndex = 1;
    else if (location.startsWith('/announcements'))
      currentIndex = 2;
    else if (location.startsWith('/profile'))
      currentIndex = 3;

    return BottomNavigationBar(
      currentIndex: currentIndex,
      onTap: (index) {
        switch (index) {
          case 0:
            context.go('/home');
            break;
          case 1:
            context.go('/schedule');
            break;
          case 2:
            context.go('/announcements');
            break;
          case 3:
            context.go('/profile');
            break;
        }
      },
      items: const [
        BottomNavigationBarItem(
          icon: Icon(Icons.home_outlined),
          activeIcon: Icon(Icons.home),
          label: 'Trang chủ',
        ),
        BottomNavigationBarItem(
          icon: Icon(Icons.calendar_month_outlined),
          activeIcon: Icon(Icons.calendar_month),
          label: 'Lịch học',
        ),
        BottomNavigationBarItem(
          icon: Icon(Icons.notifications_outlined),
          activeIcon: Icon(Icons.notifications),
          label: 'Thông báo',
        ),
        BottomNavigationBarItem(
          icon: Icon(Icons.person_outline),
          activeIcon: Icon(Icons.person),
          label: 'Cá nhân',
        ),
      ],
    );
  }
}
