// Public code/parameter contract; translations live in messages/tr.json and en.json.
export const errorCodes = {
  "BF_CONSTRUCTION_YEAR_INVALID": {"status": 400, "severity": "error", "parameters": []},
  "BF_HEATING_TYPE_INVALID": {"status": 400, "severity": "error", "parameters": []},
  "BF_PARKING_TYPE_INVALID": {"status": 400, "severity": "error", "parameters": []},
  "BF_BUILDING_FEATURES_APARTMENT_ONLY": {"status": 400, "severity": "error", "parameters": []},
  "BF_NET_AREA_INVALID": {"status": 400, "severity": "error", "parameters": []},
  "BF_ROOM_LAYOUT_INVALID": {"status": 400, "severity": "error", "parameters": []},
  "BF_FLOOR_POSITION_INVALID": {"status": 400, "severity": "error", "parameters": []},
  "BF_FLOOR_EXCEEDS_FLOOR_COUNT": {"status": 400, "severity": "error", "parameters": []},
  "BF_BATHROOM_COUNT_INVALID": {"status": 400, "severity": "error", "parameters": []},
  "BF_KITCHEN_TYPE_INVALID": {"status": 400, "severity": "error", "parameters": []},
  "BF_METER_INVALID": {"status": 400, "severity": "error", "parameters": []},
  "BF_READING_INVALID": {"status": 400, "severity": "error", "parameters": []},
  "BF_METER_CONFLICT": {"status": 409, "severity": "warning", "parameters": []},
  "BF_METER_INVALID_UNIT": {"status": 409, "severity": "warning", "parameters": []},
  "BF_METER_INACTIVE": {"status": 409, "severity": "warning", "parameters": []},
  "BF_READING_ORDER": {"status": 409, "severity": "warning", "parameters": []},

  "BF_CONTENT_INVALID": {"status": 400, "severity": "error", "parameters": []},
  "BF_CONTENT_STATE": {"status": 409, "severity": "warning", "parameters": []},
  "BF_DOCUMENT_INVALID": {"status": 400, "severity": "error", "parameters": []},
  "BF_DOCUMENT_TOO_LARGE": {"status": 413, "severity": "error", "parameters": []},
  "BF_DOCUMENT_REQUEST_CONFLICT": {"status": 409, "severity": "warning", "parameters": []},
  "BF_DOCUMENT_LIMIT": {"status": 409, "severity": "warning", "parameters": []},
  "BF_STORAGE_UNAVAILABLE": {"status": 503, "severity": "error", "parameters": []},
  "BF_CONTENT_PROPERTY_HISTORY": {"status": 409, "severity": "warning", "parameters": []},

  "BF_EXPENSE_CATEGORY_INVALID": {"status": 400, "severity": "error", "parameters": []},
  "BF_EXPENSE_DATE_INVALID": {"status": 400, "severity": "error", "parameters": []},
  "BF_EXPENSE_REFERENCE_INVALID": {"status": 400, "severity": "error", "parameters": []},
  "BF_BUDGET_AMOUNT_INVALID": {"status": 400, "severity": "error", "parameters": []},
  "BF_EXPENSE_REQUEST_CONFLICT": {"status": 409, "severity": "warning", "parameters": []},
  "BF_EXPENSE_ALREADY_CANCELLED": {"status": 409, "severity": "warning", "parameters": []},
  "BF_BUDGET_REQUEST_CONFLICT": {"status": 409, "severity": "warning", "parameters": []},

  "BF_PAYMENTS_ERROR_ALLOCATIONS": {
    "status": 400,
    "severity": "error",
    "parameters": []
  },
  "BF_DUES_ERROR_UNIT_SELECTION": {
    "status": 400,
    "severity": "error",
    "parameters": []
  },
  "BF_DUES_ERROR_PERIOD": {
    "status": 400,
    "severity": "error",
    "parameters": []
  },
  "BF_APARTMENT_NO_BUILDING": {
    "status": 409,
    "severity": "warning",
    "parameters": []
  },
  "BF_APARTMENT_DIRECT_UNITS_ONLY": {
    "status": 409,
    "severity": "warning",
    "parameters": []
  },
  "BF_ERRORS_SEARCH_BOUNDS": {
    "status": 400,
    "severity": "error",
    "parameters": []
  },
  "BF_UNIT_NUMBER_EXISTS": {
    "status": 409,
    "severity": "warning",
    "parameters": []
  },
  "BF_DUES_ERROR_DESCRIPTION": {
    "status": 400,
    "severity": "error",
    "parameters": []
  },
  "BF_UI_SELECT_AUNIT": {
    "status": 400,
    "severity": "error",
    "parameters": []
  },
  "BF_UI_ASTART_DATE_IS_REQUIRED": {
    "status": 400,
    "severity": "error",
    "parameters": []
  },
  "BF_PROPERTY_TYPE_LOCKED": {
    "status": 409,
    "severity": "warning",
    "parameters": []
  },
  "BF_UI_THE_END_DATE_CANNOT_PRECEDE_THE_START_DATE": {
    "status": 400,
    "severity": "error",
    "parameters": []
  },
  "BF_OCCUPANCY_ALREADY_ENDED": {
    "status": 409,
    "severity": "warning",
    "parameters": []
  },
  "BF_END_DATE_REQUIRED": {
    "status": 400,
    "severity": "error",
    "parameters": []
  },
  "BF_BUILDING_HAS_UNITS": {
    "status": 409,
    "severity": "warning",
    "parameters": []
  },
  "BF_ERRORS_AREA_BOUNDS": {
    "status": 400,
    "severity": "error",
    "parameters": []
  },
  "BF_BUILDING_UNIT_NUMBER_EXISTS": {
    "status": 409,
    "severity": "warning",
    "parameters": []
  },
  "BF_OCCUPANCY_OVERLAP": {
    "status": 409,
    "severity": "warning",
    "parameters": []
  },
  "BF_DUES_ERROR_CURRENCY": {
    "status": 400,
    "severity": "error",
    "parameters": []
  },
  "BF_PAYMENTS_ERROR_UNIQUE": {
    "status": 409,
    "severity": "warning",
    "parameters": []
  },
  "BF_DUES_ERROR_REQUEST_CONFLICT": {
    "status": 409,
    "severity": "warning",
    "parameters": []
  },
  "BF_PAYMENTS_ERROR_REQUEST_CONFLICT": {
    "status": 409,
    "severity": "warning",
    "parameters": []
  },
  "BF_PERSON_OCCUPANCY_OVERLAP": {
    "status": 409,
    "severity": "warning",
    "parameters": []
  },
  "BF_BUILDING_NAME_EXISTS": {
    "status": 409,
    "severity": "warning",
    "parameters": []
  },
  "BF_PRIMARY_CONTACT_OVERLAP": {
    "status": 409,
    "severity": "warning",
    "parameters": []
  },
  "BF_BUILDING_REQUIRED": {
    "status": 409,
    "severity": "warning",
    "parameters": []
  },
  "BF_DUES_ERROR_DEBTOR": {
    "status": 400,
    "severity": "error",
    "parameters": []
  },
  "BF_PAYMENTS_ERROR_BALANCE": {
    "status": 409,
    "severity": "warning",
    "parameters": []
  },
  "BF_PERSON_HAS_OCCUPANCY": {
    "status": 409,
    "severity": "warning",
    "parameters": []
  },
  "BF_PERSON_DELETE_HAS_OCCUPANCY": {
    "status": 409,
    "severity": "warning",
    "parameters": []
  },
  "BF_UNIT_HAS_OCCUPANCY": {
    "status": 409,
    "severity": "warning",
    "parameters": []
  },
  "BF_UNIT_DELETE_HAS_OCCUPANCY": {
    "status": 409,
    "severity": "warning",
    "parameters": []
  },
  "BF_DUES_OTHER_REASON_REQUIRED": {
    "status": 400,
    "severity": "error",
    "parameters": []
  },
  "BF_DUES_ERROR_APPROVAL": {
    "status": 400,
    "severity": "error",
    "parameters": []
  },
  "BF_ENABLED_REQUIRED": {
    "status": 400,
    "severity": "error",
    "parameters": []
  },
  "BF_DUES_ERROR_PROPERTY_HISTORY": {
    "status": 409,
    "severity": "warning",
    "parameters": []
  },
  "BF_DUES_ERROR_UNIT": {
    "status": 400,
    "severity": "error",
    "parameters": []
  },
  "BF_UI_ENTER_AVALID_EMAIL_ADDRESS": {
    "status": 400,
    "severity": "error",
    "parameters": []
  },
  "BF_ERRORS_VALID_PROPERTY_TYPE": {
    "status": 400,
    "severity": "error",
    "parameters": []
  },
  "BF_DUES_ERROR_STATUS": {
    "status": 400,
    "severity": "error",
    "parameters": []
  },
  "BF_DUES_ERROR_REQUEST_ID": {
    "status": 400,
    "severity": "error",
    "parameters": []
  },
  "BF_VERSION_REQUIRED": {
    "status": 400,
    "severity": "error",
    "parameters": []
  },
  "BF_DUES_ERROR_PERSON_CHOICES": {
    "status": 400,
    "severity": "error",
    "parameters": []
  },
  "BF_PERSON_INVALID": {
    "status": 400,
    "severity": "error",
    "parameters": []
  },
  "BF_DUES_ERROR_CHOICES": {
    "status": 400,
    "severity": "error",
    "parameters": []
  },
  "BF_PAYMENTS_ERROR_STATUS": {
    "status": 400,
    "severity": "error",
    "parameters": []
  },
  "BF_PAYMENTS_ERROR_DATE": {
    "status": 400,
    "severity": "error",
    "parameters": []
  },
  "BF_INVALID_PAGE": {
    "status": 400,
    "severity": "error",
    "parameters": []
  },
  "BF_INVALID_SORT_FIELD": {
    "status": 400,
    "severity": "error",
    "parameters": []
  },
  "BF_INVALID_SORT_DIRECTION": {
    "status": 400,
    "severity": "error",
    "parameters": []
  },
  "BF_PAYMENTS_REFERENCE_REQUIRED": {
    "status": 400,
    "severity": "error",
    "parameters": []
  },
  "BF_DUES_ERROR_AMBIGUOUS_DEBTOR": {
    "status": 400,
    "severity": "error",
    "parameters": []
  },
  "BF_BUILDING_FLOOR_METADATA": {
    "status": 409,
    "severity": "warning",
    "parameters": []
  },
  "BF_UI_THE_NUMBER_OF_FLOORS_MUST_BE_POSITIVE": {
    "status": 400,
    "severity": "error",
    "parameters": []
  },
  "BF_ERRORS_APARTMENT_FLOORS_ONLY": {
    "status": 400,
    "severity": "error",
    "parameters": []
  },
  "BF_ERRORS_APARTMENT_POSITIVE_FLOORS": {
    "status": 400,
    "severity": "error",
    "parameters": []
  },
  "BF_ERRORS_FLOOR_REQUIRED": {
    "status": 400,
    "severity": "error",
    "parameters": []
  },
  "BF_RECORD_CHANGED": {
    "status": 409,
    "severity": "warning",
    "parameters": []
  },
  "BF_DUES_ERROR_MOVE_OUT_REASON": {
    "status": 409,
    "severity": "warning",
    "parameters": []
  },
  "BF_PERSON_REQUIRED": {
    "status": 400,
    "severity": "error",
    "parameters": []
  },
  "BF_OCCUPANCY_TYPE_INVALID": {
    "status": 400,
    "severity": "error",
    "parameters": []
  },
  "BF_PAYMENTS_ERROR_METHOD": {
    "status": 400,
    "severity": "error",
    "parameters": []
  },
  "BF_DUES_ERROR_STALE_APPROVAL": {
    "status": 409,
    "severity": "warning",
    "parameters": []
  },
  "BF_DUES_ERROR_INACTIVE": {
    "status": 409,
    "severity": "warning",
    "parameters": []
  },
  "BF_PAYMENTS_INACTIVE_PROPERTY": {
    "status": 409,
    "severity": "warning",
    "parameters": []
  },
  "BF_ERRORS_PAGE_BOUNDS": {
    "status": 400,
    "severity": "error",
    "parameters": []
  },
  "BF_ERRORS_PAGE_SIZE_BOUNDS": {
    "status": 400,
    "severity": "error",
    "parameters": []
  },
  "BF_PAGE_SIZE_BOUNDS": {
    "status": 400,
    "severity": "error",
    "parameters": []
  },
  "BF_DUES_ERROR_DUPLICATE": {
    "status": 409,
    "severity": "warning",
    "parameters": []
  },
  "BF_DUES_ERROR_BUILDING": {
    "status": 409,
    "severity": "warning",
    "parameters": []
  },
  "BF_DUES_ERROR_DUE_RANGE": {
    "status": 400,
    "severity": "error",
    "parameters": []
  },
  "BF_PAYMENTS_ERROR_DATE_RANGE": {
    "status": 400,
    "severity": "error",
    "parameters": []
  },
  "BF_DUES_ERROR_UNIQUE": {
    "status": 409,
    "severity": "warning",
    "parameters": []
  },
  "BF_PHONE_TOO_LONG": {
    "status": 400,
    "severity": "error",
    "parameters": []
  },
  "BF_DUES_ERROR_AMOUNT": {
    "status": 400,
    "severity": "error",
    "parameters": []
  },
  "BF_DUES_ERROR_DUE": {
    "status": 400,
    "severity": "error",
    "parameters": []
  },
  "BF_PROPERTY_HAS_CHILDREN": {
    "status": 409,
    "severity": "warning",
    "parameters": []
  },
  "BF_PAYMENTS_ERROR_FUTURE_DATE": {
    "status": 400,
    "severity": "error",
    "parameters": []
  },
  "BF_PAYMENTS_ERROR_TOTAL": {
    "status": 400,
    "severity": "error",
    "parameters": []
  },
  "BF_PAYMENTS_DEBT_HAS_PAYMENTS": {
    "status": 409,
    "severity": "warning",
    "parameters": []
  },
  "BF_OCCUPANCY_PARENT_INACTIVE": {
    "status": 409,
    "severity": "warning",
    "parameters": []
  },
  "BF_PARENT_INACTIVE": {
    "status": 409,
    "severity": "warning",
    "parameters": []
  },
  "BF_DUES_ERROR_CANCELLED": {
    "status": 409,
    "severity": "warning",
    "parameters": []
  },
  "BF_PAYMENTS_ERROR_CANCELLED": {
    "status": 409,
    "severity": "warning",
    "parameters": []
  },
  "BF_DUES_ERROR_REASON": {
    "status": 400,
    "severity": "error",
    "parameters": []
  },
  "BF_DUES_CHOOSE_REASON": {
    "status": 400,
    "severity": "error",
    "parameters": []
  },
  "BF_VALIDATION_FAILED": {
    "status": 400,
    "severity": "error",
    "parameters": []
  },
  "BF_INVALID_REQUEST": {
    "status": 400,
    "severity": "error",
    "parameters": []
  },
  "BF_SIGN_IN_REQUIRED": {
    "status": 401,
    "severity": "error",
    "parameters": []
  },
  "BF_ACCESS_DENIED": {
    "status": 403,
    "severity": "error",
    "parameters": []
  },
  "BF_RESOURCE_NOT_FOUND": {
    "status": 404,
    "severity": "error",
    "parameters": []
  },
  "BF_UNEXPECTED_FAILURE": {
    "status": 500,
    "severity": "error",
    "parameters": []
  },
  "BF_METHOD_NOT_ALLOWED": {
    "status": 405,
    "severity": "error",
    "parameters": []
  },
  "BF_UNSUPPORTED_MEDIA": {
    "status": 415,
    "severity": "error",
    "parameters": []
  },
  "BF_RATE_LIMITED": {
    "status": 429,
    "severity": "error",
    "parameters": []
  },
  "BF_FIELD_REQUIRED": {
    "status": 400,
    "severity": "error",
    "parameters": [
      "field"
    ]
  },
  "BF_FIELD_TOO_LONG": {
    "status": 400,
    "severity": "error",
    "parameters": [
      "field",
      "max"
    ]
  },
  "BF_FIELD_REQUIRED_MAX": {
    "status": 400,
    "severity": "error",
    "parameters": [
      "field",
      "max"
    ]
  }
} as const;
