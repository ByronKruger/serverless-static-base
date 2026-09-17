---
title: Beverage Preference Management
domain: Beverage Preferences
topics:
  - Create beverage preference
  - Edit beverage preference
  - Delete a beverage preference
codeReferences:
  - 
generatedDate: 2026-04-24
lastVerified: 2026-04-28
---

# Overview of Beverage Preferences

Each user can save their own beverage preference. A beverage preference has a beverage type (e.g. coffee, whiskey, tea, etc.). Each beverage type is composed of ingredients. An ingredient is itself either simple (has a description only) or is complex: a description and a related set of the complex ingredients. For instance, coffee is a simple ingredient of only one type, whereas milk ingredient is complex and can either be low-fat milk, raw milk, soy milk, etc. For any preference, an amount per ingredient is saved (e.g. tea has milk, sugar and water and for each ingredient an amount: `yes` milk, 2 teaspoons of honey which is a complex ingredient of type sugar). Each ingredient must also have an associated unit type (E.g. percentage, <`yes`|`no`>, teaspoons, shot e.g. whiskey). A user is only allowed to have one preference with a specific beverage type. If a user does not want a specific ingredient in their beverage they specify a null/empty value and that means (0 teaspoons, `No`, 0% etc.)