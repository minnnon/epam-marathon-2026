@api @hw8 @level-1 @group-preorder
Feature: Group pre-order participants

  A customer who books a table for a group invites the other people to pre-order.
  Each guest joins through the invite and fills a cart of their own. Before the
  pre-order goes to the restaurant, the owner of the booking sees who joined,
  what each of them chose and how much it comes to. Nobody else may see that.

  The dishes come from today's menu and every guest gets a fresh session, so the
  expected portions and amounts are the ones the guests themselves put in, never
  numbers written down in advance.

  Background:
    Given a customer has booked a table for a group of 4

  Rule: The owner sees who joined and what each of them chose

    @smoke
    Scenario: The owner sees every guest with their dishes, portions and subtotal
      Given these guests have joined the group:
        | guest | different dishes | portions of each | cart      |
        | Anna  | 2                | 2                | submitted |
        | Bob   | 1                | 1                | submitted |
      When the owner looks at the group's participants
      Then the owner sees every guest with their dishes, portions and subtotal

    Scenario: The group total is the sum of the guests' subtotals
      Given these guests have joined the group:
        | guest | different dishes | portions of each | cart      |
        | Anna  | 2                | 1                | submitted |
        | Bob   | 1                | 3                | open      |
      When the owner looks at the group's participants
      Then the group total is the sum of the guests' subtotals

    Scenario Outline: The group total follows a guest's cart
      Given these guests have joined the group:
        | guest | different dishes | portions of each | cart      |
        | Anna  | 1                | 2                | submitted |
        | Bob   | 1                | 2                | open      |
      And the owner has seen the group total
      When Bob <changes the cart>
      And the owner looks at the group's participants
      Then the group total has changed by the price of that portion
      And the group total is the sum of the guests' subtotals

      Examples: Both directions
        | changes the cart                 |
        | adds another portion to the cart |
        | takes a portion out of the cart  |

    Scenario: A guest who is still choosing is marked as not having submitted the cart
      Given these guests have joined the group:
        | guest | different dishes | portions of each | cart      |
        | Anna  | 1                | 1                | submitted |
        | Bob   | 1                | 1                | open      |
      When the owner looks at the group's participants
      Then Anna is shown as having submitted the cart
      And Bob is shown as still choosing

    Scenario: A guest who has not chosen anything yet is still listed
      Given these guests have joined the group:
        | guest | different dishes | portions of each | cart      |
        | Anna  | 1                | 1                | submitted |
        | Carl  | 0                | 0                | open      |
      When the owner looks at the group's participants
      Then Carl is shown with no dishes and nothing to pay
      And Carl is shown as still choosing

    Scenario: Nobody has joined the group yet
      When the owner looks at the group's participants
      Then the owner sees that nobody has joined yet

  Rule: Only the owner of the booking can see the participants

    Background:
      Given these guests have joined the group:
        | guest | different dishes | portions of each | cart      |
        | Anna  | 1                | 1                | submitted |

    @negative
    Scenario Outline: Someone other than the owner cannot see the participants
      When <someone> tries to look at the group's participants
      Then they are not allowed to see the participants

      Examples: Customers and guests
        | someone                        |
        | another customer               |
        | a guest of the group           |
        | a visitor who is not signed in |

      # Needs WAITER_USERNAME / WAITER_PASSWORD; skipped when no waiter account is configured.
      @requires-waiter
      Examples: Restaurant staff
        | someone  |
        | a waiter |

  Rule: The owner is told when the group does not exist

    @negative
    Scenario: The owner asks about a group that does not exist
      When the owner looks at the participants of a group that does not exist
      Then the owner is told that the group could not be found
