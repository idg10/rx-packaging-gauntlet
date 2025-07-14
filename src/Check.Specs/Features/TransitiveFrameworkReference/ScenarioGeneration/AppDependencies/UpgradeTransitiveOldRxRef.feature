Feature: Upgrading when Rx dependency acquired through a transitive reference

Background:
    Given I get all the App Dependency combinations for upgrading an old Rx reference acquired transitively

Scenario: Upgrade transitive ref by adding a reference to new main Rx package
    Given only the scenarios where Before App Dependencies are exactly
        | Dependency    |
        | LibUsingOldRx |
    And only the scenarios where Before App Dependencies are exactly
        | Dependency    |
        | LibUsingOldRx |
        | NewRxMain     |
    Then at least one matching scenario must exist
    #And all matching scenarios expect library code to get main Rx types from 'OldRx' 
    #And all matching scenarios expect library code to get UI Rx types from 'OldRx'

# TODO:
#   Rx should cause legacy package update suggestion (for designs where System.Reactive is a legacy package)
#   Should be able to suppress legacy package update suggestion
#   Presence of and ability to suppress NuGet legacy package warning?

# TODO:
#   Scenarios should express expectation around 'two Rx versions' scenario


Scenario: Upgrade transitive ref by adding a reference to new Rx legacy package
    Given only the scenarios where Before App Dependencies are exactly
        | Dependency    |
        | LibUsingOldRx |
    And only the scenarios where Before App Dependencies are exactly
        | Dependency        |
        | LibUsingOldRx     |
        | NewRxLegacyFacade |
    Then at least one matching scenario must exist
    #And all matching scenarios expect library code to get main Rx types from 'NewRxMain' 
    #And all matching scenarios expect library code to get UI Rx types from 'NewRxLegacyFacade'

Scenario: Upgrade transitive ref by adding references to new Rx main and legacy packages
    Given only the scenarios where Before App Dependencies are exactly
        | Dependency    |
        | LibUsingOldRx |
    And only the scenarios where Before App Dependencies are exactly
        | Dependency    |
        | LibUsingOldRx |
        | NewRxMain     |
        | NewRxLegacyFacade |
    Then at least one scenario's After App Dependencies should reference new Rx's main and legacy facade packages

    
Scenario: 
    Given only the scenarios where Before App Dependencies include a library that references old Rx
    And only the scenarios where the Before App Dependencies reference old Rx directly

# group by?
Scenario: Windows-specific and non-OS-specific TFMs
    #Then the Before App Dependencies contain the following distinct TFM lists
    #| net8.0                          |
    #| net8.0-windows10.0.19041        |
    #| net8.0;net8.0-windows10.0.19041 |
    #And each combination whose TFM list includes a windows-specific TFM offers the f