@api @hw8 @level-3 @group-preorder
Feature: Removing a participant from a group pre-order

  The owner of a group booking can take a guest out of the group pre-order — for
  example someone who is not coming after all. Afterwards the owner sees the
  remaining guests and a total that no longer counts the removed guest's dishes.
  Only a guest who belongs to this very group, and who exists, can be removed,
  and only the owner can remove anyone.

  Background:
    Given a customer has booked a table for a group of 4
    And these guests have joined the group:
      | guest | different dishes | portions of each | cart      |
      | Anna  | 2                | 1                | submitted |
      | Bob   | 1                | 2                | open      |
      | Carl  | 1                | 1                | submitted |

  Rule: The owner can remove a guest of the group

    @smoke
    Scenario: Removing one guest of several
      When the owner removes Bob from the group
      And the owner looks at the group's participants
      Then Bob is no longer a participant
      And the owner sees every guest with their dishes, portions and subtotal
      And the group total is the sum of the guests' subtotals

    Scenario: The owner is shown the updated group straight after a removal
      When the owner removes Bob from the group
      Then Bob is no longer a participant
      And the group total is the sum of the guests' subtotals

    Scenario: Removing a guest who has already submitted the cart
      When the owner removes Anna from the group
      And the owner looks at the group's participants
      Then Anna is no longer a participant
      And the group total is the sum of the guests' subtotals

    Scenario: Removing the last guest leaves an empty group
      When the owner removes every guest from the group
      And the owner looks at the group's participants
      Then the owner sees that nobody has joined yet

  Rule: Only a guest who exists and belongs to this group can be removed

    @negative
    Scenario: A guest of another group cannot be removed from this one
      Given another customer has a group of their own that Xenia has joined
      When the owner tries to remove Xenia from the group
      Then the owner is told that Xenia is not part of this group
      And every guest of the group is still a participant

    @negative
    Scenario: A guest who does not exist cannot be removed
      When the owner tries to remove a guest who does not exist
      Then the owner is told that the guest could not be found

    @negative
    Scenario: A guest cannot be removed twice
      Given the owner has removed Bob from the group
      When the owner tries to remove Bob from the group
      Then the owner is told that the guest could not be found

  Rule: Only the owner of the booking can remove a guest

    @negative
    Scenario Outline: Someone other than the owner cannot remove a guest
      When <someone> tries to remove Bob from the group
      Then they are not allowed to remove anyone
      And every guest of the group is still a participant

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
