@ui @hw8 @ui-smoke
Feature: Signing in on the storefront

  The one browser check of this suite: a customer who registered can sign in on
  the storefront, and a wrong password keeps them signed out.

  Background:
    Given a customer has registered

  Rule: A registered customer can sign in

    @smoke
    Scenario: A registered customer signs in on the storefront
      When the customer signs in on the storefront
      Then the storefront shows the customer as signed in

  Rule: A wrong password is refused

    @negative
    Scenario: A customer who gets the password wrong stays signed out
      When the customer signs in on the storefront with a wrong password
      Then the customer is still asked to sign in
