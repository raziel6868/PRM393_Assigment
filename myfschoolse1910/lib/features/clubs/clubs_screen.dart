import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../core/auth.dart';
import '../../shared/api_view.dart';
import '../../shared/theme.dart';

class ClubsScreen extends ConsumerStatefulWidget {
  const ClubsScreen({super.key});

  @override
  ConsumerState<ClubsScreen> createState() => _ClubsScreenState();
}

class _ClubsScreenState extends ConsumerState<ClubsScreen> {
  final _searchController = TextEditingController();
  String? _category;
  late Future<List<Map<String, dynamic>>> _clubs;

  @override
  void initState() {
    super.initState();
    _clubs = _load();
  }

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  Future<List<Map<String, dynamic>>> _load() async {
    final response = await ref
        .read(apiClientProvider)
        .getClubs(
          search: _searchController.text.trim().isEmpty
              ? null
              : _searchController.text.trim(),
          category: _category,
        );
    return asMapList(asMap(response.data)['items'] ?? const []);
  }

  void _reload() => setState(() => _clubs = _load());

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Câu lạc bộ')),
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 16, 16, 8),
            child: TextField(
              controller: _searchController,
              textInputAction: TextInputAction.search,
              onSubmitted: (_) => _reload(),
              decoration: InputDecoration(
                hintText: 'Tìm câu lạc bộ...',
                prefixIcon: const Icon(Icons.search),
                suffixIcon: IconButton(
                  onPressed: _reload,
                  icon: const Icon(Icons.arrow_forward),
                ),
              ),
            ),
          ),
          SizedBox(
            height: 48,
            child: ListView(
              padding: const EdgeInsets.symmetric(horizontal: 16),
              scrollDirection: Axis.horizontal,
              children: [
                _CategoryChip(
                  label: 'Tất cả',
                  selected: _category == null,
                  onTap: () {
                    _category = null;
                    _reload();
                  },
                ),
                for (final item in const [
                  'Học thuật',
                  'Thể thao',
                  'Nghệ thuật',
                  'Công nghệ',
                ])
                  _CategoryChip(
                    label: item,
                    selected: _category == item,
                    onTap: () {
                      _category = item;
                      _reload();
                    },
                  ),
              ],
            ),
          ),
          Expanded(
            child: FutureBuilder<List<Map<String, dynamic>>>(
              future: _clubs,
              builder: (context, snapshot) {
                if (snapshot.connectionState != ConnectionState.done) {
                  return const LoadingView();
                }
                if (snapshot.hasError) {
                  return ErrorView(error: snapshot.error!, onRetry: _reload);
                }
                final clubs = snapshot.data ?? [];
                if (clubs.isEmpty) {
                  return const EmptyView(
                    message: 'Không tìm thấy câu lạc bộ phù hợp.',
                    icon: Icons.groups_outlined,
                  );
                }
                return RefreshIndicator(
                  onRefresh: () async => _reload(),
                  child: ListView.separated(
                    padding: const EdgeInsets.fromLTRB(16, 8, 16, 24),
                    itemCount: clubs.length,
                    separatorBuilder: (_, _) => const SizedBox(height: 10),
                    itemBuilder: (context, index) {
                      final club = clubs[index];
                      return Card(
                        child: ListTile(
                          contentPadding: const EdgeInsets.all(14),
                          leading: const CircleAvatar(
                            radius: 28,
                            backgroundColor: AppTheme.orangeTint,
                            child: Icon(
                              Icons.groups_outlined,
                              color: AppTheme.primaryOrange,
                            ),
                          ),
                          title: Text(
                            club['displayName']?.toString() ?? 'Câu lạc bộ',
                            style: const TextStyle(fontWeight: FontWeight.w700),
                          ),
                          subtitle: Padding(
                            padding: const EdgeInsets.only(top: 6),
                            child: Text(
                              '${club['category']} · '
                              '${club['currentMemberCount']} thành viên',
                            ),
                          ),
                          trailing: const Icon(Icons.chevron_right),
                          onTap: () => context.push('/clubs/${club['id']}'),
                        ),
                      );
                    },
                  ),
                );
              },
            ),
          ),
        ],
      ),
    );
  }
}

class _CategoryChip extends StatelessWidget {
  final String label;
  final bool selected;
  final VoidCallback onTap;

  const _CategoryChip({
    required this.label,
    required this.selected,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(right: 8),
      child: ChoiceChip(
        label: Text(label),
        selected: selected,
        onSelected: (_) => onTap(),
      ),
    );
  }
}
