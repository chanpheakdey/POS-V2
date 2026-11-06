/// Example usage of AuthService
/// 
/// This file demonstrates how to use the authentication service in widgets.
/// DO NOT USE IN PRODUCTION - for reference only.

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'auth_service.dart';
import 'auth_state.dart';
import 'auth_result.dart';

/// Example login screen showing AuthService integration
class ExampleLoginScreen extends ConsumerStatefulWidget {
  const ExampleLoginScreen({super.key});

  @override
  ConsumerState<ExampleLoginScreen> createState() => _ExampleLoginScreenState();
}

class _ExampleLoginScreenState extends ConsumerState<ExampleLoginScreen> {
  final _usernameController = TextEditingController();
  final _passwordController = TextEditingController();
  bool _isLoading = false;

  @override
  void dispose() {
    _usernameController.dispose();
    _passwordController.dispose();
    super.dispose();
  }

  Future<void> _handleLogin() async {
    setState(() => _isLoading = true);

    try {
      final authService = ref.read(authServiceProvider.notifier);
      final result = await authService.login(
        _usernameController.text,
        _passwordController.text,
      );

      if (!mounted) return;

      switch (result) {
        case AuthSuccess():
          // Navigate to home screen
          // Navigator.pushReplacement(context, MaterialPageRoute(builder: (_) => HomeScreen()));
          _showSnackBar('Login successful!', isError: false);
          
        case AuthFailure(message: final msg):
          _showSnackBar(msg, isError: true);
          
        case AuthAccountLocked():
          _showSnackBar(
            'Your user account has been locked! Please contact IT',
            isError: true,
          );
          
        case AuthInvalidUserLevel():
          _showSnackBar(
            'Only customer accounts can access this application',
            isError: true,
          );
          
        case AuthNetworkError(message: final msg):
          _showSnackBar('Network error: $msg', isError: true);
      }
    } finally {
      if (mounted) {
        setState(() => _isLoading = false);
      }
    }
  }

  void _showSnackBar(String message, {required bool isError}) {
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text(message),
        backgroundColor: isError ? Colors.red : Colors.green,
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    // Watch the auth state
    final authState = ref.watch(authServiceProvider);

    return Scaffold(
      appBar: AppBar(title: const Text('Login')),
      body: authState.when(
        data: (state) {
          if (state is Authenticated) {
            return Center(
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  Text('Welcome, ${state.username}!'),
                  const SizedBox(height: 16),
                  ElevatedButton(
                    onPressed: () async {
                      final authService = ref.read(authServiceProvider.notifier);
                      await authService.logout();
                    },
                    child: const Text('Logout'),
                  ),
                ],
              ),
            );
          }

          return Padding(
            padding: const EdgeInsets.all(16.0),
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                TextField(
                  controller: _usernameController,
                  decoration: const InputDecoration(
                    labelText: 'Username',
                    border: OutlineInputBorder(),
                  ),
                  enabled: !_isLoading,
                ),
                const SizedBox(height: 16),
                TextField(
                  controller: _passwordController,
                  decoration: const InputDecoration(
                    labelText: 'Password',
                    border: OutlineInputBorder(),
                  ),
                  obscureText: true,
                  enabled: !_isLoading,
                ),
                const SizedBox(height: 24),
                SizedBox(
                  width: double.infinity,
                  child: ElevatedButton(
                    onPressed: _isLoading ? null : _handleLogin,
                    child: _isLoading
                        ? const SizedBox(
                            height: 20,
                            width: 20,
                            child: CircularProgressIndicator(strokeWidth: 2),
                          )
                        : const Text('Login'),
                  ),
                ),
              ],
            ),
          );
        },
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (error, stack) => Center(
          child: Text('Error: $error'),
        ),
      ),
    );
  }
}

/// Example home screen showing how to access current user info
class ExampleHomeScreen extends ConsumerWidget {
  const ExampleHomeScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final authState = ref.watch(authServiceProvider);

    return authState.when(
      data: (state) {
        if (state is! Authenticated) {
          // Not authenticated, should redirect to login
          return const Center(child: Text('Not authenticated'));
        }

        final authService = ref.read(authServiceProvider.notifier);
        
        return Scaffold(
          appBar: AppBar(
            title: const Text('Home'),
            actions: [
              IconButton(
                icon: const Icon(Icons.logout),
                onPressed: () async {
                  await authService.logout();
                  // Navigate to login screen
                },
              ),
            ],
          ),
          body: Padding(
            padding: const EdgeInsets.all(16.0),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  'Welcome, ${state.username}!',
                  style: Theme.of(context).textTheme.headlineMedium,
                ),
                const SizedBox(height: 16),
                Text('User ID: ${state.userId}'),
                Text('Token: ${state.token.substring(0, 20)}...'),
                const SizedBox(height: 24),
                const Text('You are authenticated and can access the app.'),
              ],
            ),
          ),
        );
      },
      loading: () => const Scaffold(
        body: Center(child: CircularProgressIndicator()),
      ),
      error: (error, stack) => Scaffold(
        body: Center(child: Text('Error: $error')),
      ),
    );
  }
}

/// Example of checking authentication in a route guard
class ExampleAuthGuard {
  static bool isAuthenticated(WidgetRef ref) {
    final authState = ref.read(authServiceProvider).value;
    return authState is Authenticated;
  }

  static int? getUserId(WidgetRef ref) {
    final authService = ref.read(authServiceProvider.notifier);
    return authService.currentUserId;
  }

  static String? getToken(WidgetRef ref) {
    final authService = ref.read(authServiceProvider.notifier);
    return authService.currentToken;
  }
}
