import { test as base, expect, Page } from '@playwright/test'

const baseURL = 'http://localhost:5173'

// --- Helper functions ---
async function register(page: Page, email: string, password: string) {
  await page.goto(baseURL)
  await page.click('text=Register here')
  await page.fill('input[placeholder="Email"]', email)
  await page.fill('input[placeholder="Password"]', password)
  await page.fill('input[placeholder="Confirm Password"]', password)
  await Promise.all([
    page.waitForResponse((resp) =>
      resp.url().includes('/auth/register') && resp.status() === 200
    ),
    page.click('button:has-text("Register")'),
  ])
}

async function login(page: Page, email: string, password: string) {
  await page.goto(baseURL)
  await page.fill('input[placeholder="Email"]', email)
  await page.fill('input[placeholder="Password"]', password)
  await Promise.all([
    page.waitForResponse((resp) =>
      resp.url().includes('/auth/login') && resp.status() === 200
    ),
    page.click('button:has-text("Login")'),
  ])
}

async function logout(page: Page) {
  await page.click('button:has-text("Logout")')
  await page.waitForURL('**/login')
}

// --- Extend test with a login fixture ---
const test = base.extend<{
  loggedInPage: Page
}>({
  loggedInPage: async ({ page }, use) => {
    const email = `fixture+${Date.now()}@example.com`
    const password = 'MyPassword123!'

    // Register and stay logged in
    await register(page, email, password)
    await expect(page.locator('button:has-text("Logout")')).toBeVisible()

    // Save localStorage session
    const storage = await page.context().storageState()

    // Reuse that session for next tests
    const newContext = await page.context().browser()?.newContext({
      storageState: storage,
    })
    const loggedInPage = await newContext?.newPage()
    await loggedInPage?.goto(baseURL)

    await use(loggedInPage!)

    await newContext?.close()
  },
})

// --- Test Suite ---
test.describe('Travel Wishlist Auth Flow', () => {
  test('should register new user', async ({ page }) => {
    const email = `test+${Date.now()}@example.com`
    const password = 'MyPassword123!'
    await register(page, email, password)
    const token = await page.evaluate(() => localStorage.getItem('accessToken'))
    expect(token).toBeTruthy()
  })

  test('should show error for invalid login', async ({ page }) => {
    await page.goto(baseURL)
    await page.fill('input[placeholder="Email"]', 'fake@example.com')
    await page.fill('input[placeholder="Password"]', 'WrongPass!')
    await page.click('button:has-text("Login")')
    await expect(page.locator('text=Invalid')).toBeVisible()
  })

  test('should show password validation message on weak password', async ({ page }) => {
    await page.goto(baseURL)
    await page.click('text=Register here')

    const weakPassword = 'password'
    const testEmail = `weak+${Date.now()}@example.com`

    await page.fill('input[placeholder="Email"]', testEmail)
    await page.fill('input[placeholder="Password"]', weakPassword)
    await page.fill('input[placeholder="Confirm Password"]', weakPassword)
    await page.click('button:has-text("Register")')

    await expect(
      page.locator('text=Password must be at least 8 characters')
    ).toBeVisible()
  })
  
  test('should persist session after reload', async ({ loggedInPage }) => {
    await loggedInPage.reload()
    await expect(
      loggedInPage.locator('button:has-text("Logout")')
    ).toBeVisible()
  })

  test('should logout successfully', async ({ loggedInPage }) => {
    await logout(loggedInPage)
    await expect(loggedInPage).toHaveURL(/login/i)
    const token = await loggedInPage.evaluate(() =>
      localStorage.getItem('accessToken')
    )
    expect(token).toBeNull()
  })
})
