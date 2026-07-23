import { test, expect, type Page } from '@playwright/test';

const administratorUserName = process.env.QA_ADMIN_USERNAME;
const administratorPassword = process.env.QA_ADMIN_PASSWORD;

if (!administratorUserName || !administratorPassword) {
  throw new Error('QA_ADMIN_USERNAME and QA_ADMIN_PASSWORD are required.');
}

async function signOut(page: Page): Promise<void> {
  await page.context().clearCookies();
  await page.goto('/login');
  await page.evaluate(() => {
    window.sessionStorage.clear();
  });
}

test.describe('@web-auth portal authentication', () => {
  test('administrator sign-in redirects to dashboard', async ({ page }) => {
    await page.goto('/login');
    await page.getByLabel('Email hoặc mã tài khoản').fill(administratorUserName!);
    await page.getByLabel('Mật khẩu', { exact: false }).first().fill(administratorPassword!);
    await page.getByRole('button', { name: 'Đăng nhập' }).click();

    await page.waitForURL(/\/dashboard$/);
    await expect(page.getByText(administratorUserName!)).toBeVisible();
  });

  test('invalid credentials show inline error without navigation', async ({ page }) => {
    await page.goto('/login');
    await page.getByLabel('Email hoặc mã tài khoản').fill('unknown-account');
    await page.getByLabel('Mật khẩu', { exact: false }).first().fill('Wrong-Password-1!');
    await page.getByRole('button', { name: 'Đăng nhập' }).click();

    await expect(page.getByRole('alert')).toContainText(/không đúng|không thành công/i);
    await expect(page).toHaveURL(/\/login$/);
  });

  test('restricted session cannot reach dashboard; must use change-temporary-password', async ({ page, request }) => {
    // Create a Teacher through the API to obtain a temporary password + restricted session.
    const provisionResponse = await request.post('/api/v1/admin/users', {
      data: {
        displayName: 'Giáo viên Web QA',
        userName: `web-teacher-${Date.now()}`,
        email: `web-teacher-${Date.now()}@example.invalid`,
        roles: ['teacher'],
      },
      headers: { origin: 'http://localhost:5173', authorization: `Bearer ${await obtainAdminToken(request)}` },
    });
    expect(provisionResponse.status()).toBe(201);
    const provisioned = await provisionResponse.json();

    const restrictedSignIn = await request.post('/api/v1/auth/sign-in', {
      data: {
        emailOrUserName: provisioned.userName,
        password: provisioned.temporaryPassword,
        clientType: 'web',
      },
      headers: { origin: 'http://localhost:5173' },
    });
    expect(restrictedSignIn.status()).toBe(200);
    const restrictedSession = await restrictedSignIn.json();
    expect(restrictedSession.passwordChangeRequired).toBe(true);

    await page.goto('/login');
    await page.getByLabel('Email hoặc mã tài khoản').fill(provisioned.userName);
    await page.getByLabel('Mật khẩu', { exact: false }).first().fill(provisioned.temporaryPassword);
    await page.getByRole('button', { name: 'Đăng nhập' }).click();

    await page.waitForURL(/\/change-temporary-password$/);
    await expect(page.getByRole('heading', { name: 'Đổi mật khẩu tạm thời' })).toBeVisible();
  });
});

async function obtainAdminToken(request: import('@playwright/test').APIRequestContext): Promise<string> {
  const response = await request.post('/api/v1/auth/sign-in', {
    data: {
      emailOrUserName: administratorUserName!,
      password: administratorPassword!,
      clientType: 'web',
    },
    headers: { origin: 'http://localhost:5173' },
  });
  if (response.status() !== 200) {
    throw new Error(`Failed to sign in admin (${response.status()}): ${await response.text()}`);
  }
  const body = await response.json();
  return body.accessToken as string;
}

test.beforeEach(async ({ page }) => {
  await signOut(page);
});
