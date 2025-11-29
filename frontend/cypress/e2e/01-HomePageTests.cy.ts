/// <reference types="cypress" />
// Ovo iznad ubacim kako bi cy mogo da koristim.

describe('Homepage (Public)', () => {
  it('should load homepage without authentication', () => {
    cy.visit('/')
    
    // Check homepage loads
    cy.get('body').should('be.visible')
    
    // Look for key elements (replace with your actual content)
    cy.contains('Welcome').should('be.visible') // or your app name
    
    // Check that login/register options are visible
    cy.contains('Login').should('be.visible')
    cy.contains('Register').should('be.visible') // or "Sign Up"
  })

  it('should redirect to login when trying to access protected routes', () => {
    // Try to visit a protected route directly
    cy.visit('/dashboard', { failOnStatusCode: false })
    
    // Should redirect to login page
    cy.url().should('include', '/login')
    // OR check for login form if it's on the same page
    cy.get('[data-testid="login-form"]').should('be.visible')
  })
})