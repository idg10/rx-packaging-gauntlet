Feature: Upgrading when Rx dependency acquired through a transitive reference

Background:
    Given I get all the App Dependency combinations for upgrading an old Rx reference acquired transitively

# Same Transitive Reference in Before and After
Scenario: Rx upgrade does not require change to transitive reference
    Then for each combination, the Before App Dependencies contain just one entry, which I label 'libRef'
    And for each combination, the 'libRef' App Dependency is a TransitiveRxReferenceViaLibrary
    And for each combination, the After App Dependencies contain an entry identical to 'libRef'
    
# group by?
Scenario: Windows-specific and non-OS-specific TFMs
    Then the Before App Dependencies contain the following distinct TFM lists
    | net8.0                          |
    | net8.0-windows10.0.19041        |
    | net8.0;net8.0-windows10.0.19041 |
    And each combination whose TFM list includes a windows-specific TFM offers the f