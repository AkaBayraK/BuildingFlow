// Public code/parameter contract; translations live in messages/tr.json and en.json.
export const errorCodes = {
  "BF_COMMON_AREA_MAINTENANCE_INVALID":{"status":400,"severity":"error","parameters":[]},
  "BF_COMMON_AREA_MAINTENANCE_CONFLICT":{"status":409,"severity":"warning","parameters":[]},
  "BF_COMMON_AREA_RESERVATION_CLOSED":{"status":409,"severity":"warning","parameters":[]},
  "BF_COMMON_AREA_RESERVATION_FULL":{"status":409,"severity":"warning","parameters":[]},
  "BF_COMMON_AREA_RESERVATION_INVALID":{"status":400,"severity":"error","parameters":[]},
  "BF_COMMON_AREA_INVALID":{"status":400,"severity":"error","parameters":[]},
  "BF_COMMON_AREA_CONFLICT":{"status":409,"severity":"warning","parameters":[]},
  "BF_WEBSITE_DOMAIN_INVALID":{"status":400,"severity":"error","parameters":[]},
  "BF_WEBSITE_DOMAIN_CONFLICT":{"status":409,"severity":"warning","parameters":[]},
  "BF_WEBSITE_DOMAIN_UNVERIFIED":{"status":409,"severity":"warning","parameters":[]},
  "BF_WEBSITE_PREVIEW_UNAVAILABLE":{"status":409,"severity":"warning","parameters":[]},
  "BF_LATE_PENALTY_INVALID":{"status":400,"severity":"error","parameters":[]},
  "BF_LATE_PENALTY_OVERLAP":{"status":409,"severity":"warning","parameters":[]},
  "BF_LATE_PENALTY_APPROVAL":{"status":409,"severity":"warning","parameters":[]},
  "BF_LATE_PENALTY_STATE":{"status":409,"severity":"warning","parameters":[]},
  "BF_LATE_PENALTY_CRYSTALLIZED":{"status":409,"severity":"warning","parameters":[]},
  "BF_LATE_PENALTY_SWITCH_INVALID":{"status":400,"severity":"error","parameters":[]},
  "BF_LATE_PENALTY_REQUEST_CONFLICT":{"status":409,"severity":"warning","parameters":[]},
  "BF_LATE_PENALTY_REBUILD_BLOCKED":{"status":409,"severity":"warning","parameters":[]},
  "BF_LATE_PENALTY_BALANCE":{"status":409,"severity":"warning","parameters":[]},
  "BF_LATE_PENALTY_WAIVE":{"status":409,"severity":"warning","parameters":[]},
  "BF_LATE_PENALTY_DATE":{"status":400,"severity":"error","parameters":[]},
  "BF_LATE_PENALTY_DOCUMENT":{"status":400,"severity":"error","parameters":[]},
  "BF_LATE_PENALTY_CHARGE_PAID":{"status":409,"severity":"warning","parameters":[]},
  "BF_DUES_KIND_INVALID":{"status":400,"severity":"error","parameters":[]},
  "BF_PAYMENT_SETTINGS_INVALID":{"status":400,"severity":"error","parameters":[]},
  "BF_PAYMENT_BANK_ACCOUNT_INVALID":{"status":400,"severity":"error","parameters":[]},
  "BF_PAYMENT_IBAN_INVALID":{"status":400,"severity":"error","parameters":[]},
  "BF_PAYMENT_IBAN_DUPLICATE":{"status":409,"severity":"warning","parameters":[]},
  "BF_PAYMENT_BANK_ACCOUNT_STATE":{"status":409,"severity":"warning","parameters":[]},
  "BF_PAYMENT_SECOND_APPROVER":{"status":409,"severity":"warning","parameters":[]},
  "BF_PAYMENT_BANK_ACCOUNT_UNUSABLE":{"status":409,"severity":"warning","parameters":[]},
  "BF_PAYMENT_MERCHANT_INVALID":{"status":400,"severity":"error","parameters":[]},
  "BF_PAYMENT_LIVE_DISABLED":{"status":409,"severity":"warning","parameters":[]},
  "BF_PAYMENT_PROVIDER_UNAVAILABLE":{"status":409,"severity":"warning","parameters":[]},
  "BF_PAYMENT_PROVIDER_FAILURE":{"status":409,"severity":"warning","parameters":[]},
  "BF_PAYMENT_ONLINE_DISABLED":{"status":409,"severity":"warning","parameters":[]},
  "BF_PAYMENT_INTENT_INVALID":{"status":400,"severity":"error","parameters":[]},
  "BF_PAYMENT_INTENT_ACTIVE":{"status":409,"severity":"warning","parameters":[]},
  "BF_PAYMENT_NOTHING_DUE":{"status":409,"severity":"warning","parameters":[]},
  "BF_PAYMENT_PARTIAL_DISABLED":{"status":409,"severity":"warning","parameters":[]},
  "BF_PAYMENT_PARTIAL_AMOUNT":{"status":400,"severity":"error","parameters":[]},
  "BF_PAYMENT_INTENT_STATE":{"status":409,"severity":"warning","parameters":[]},
  "BF_PAYMENT_WEBHOOK_INVALID":{"status":400,"severity":"error","parameters":[]},
  "BF_PAYMENT_REFUND_INVALID":{"status":400,"severity":"error","parameters":[]},
  "BF_PAYMENT_REFUND_STATE":{"status":409,"severity":"warning","parameters":[]},
  "BF_PAYMENT_ONLINE_CANCEL":{"status":409,"severity":"warning","parameters":[]},
  "BF_PAYMENT_REVIEW_UNALLOCATABLE":{"status":409,"severity":"warning","parameters":[]},
  "BF_PAYMENT_RECONCILIATION_STATE":{"status":409,"severity":"warning","parameters":[]},
  "BF_QA_RUN_ACTIVE":{"status":409,"severity":"warning","parameters":[]},
  "BF_QA_RUN_STATE":{"status":409,"severity":"warning","parameters":[]},
  "BF_QA_REQUEST_REUSED":{"status":409,"severity":"error","parameters":[]},
  "BF_QA_RETRY_LIMIT":{"status":409,"severity":"warning","parameters":[]},
  "BF_QA_AI_UNAVAILABLE":{"status":503,"severity":"warning","parameters":[]},
  "BF_QA_AI_PLAN_INVALID":{"status":502,"severity":"warning","parameters":[]},
  "BF_QA_AI_BUDGET":{"status":429,"severity":"warning","parameters":[]},
  "BF_QA_AI_PROMPT_INVALID":{"status":400,"severity":"error","parameters":[]},

  "BF_INQUIRY_REJECTED": {"status":400,"severity":"error","parameters":[]},
  "BF_INQUIRY_UNAVAILABLE": {"status":409,"severity":"warning","parameters":[]},
  "BF_LISTING_INVALID": {"status": 400, "severity": "error", "parameters": []},
  "BF_LISTING_STATE_INVALID": {"status": 409, "severity": "warning", "parameters": []},
  "BF_LISTING_CONSENT_REQUIRED": {"status": 409, "severity": "warning", "parameters": []},
  "BF_LISTING_COMPLIANCE_REQUIRED": {"status": 409, "severity": "warning", "parameters": []},
  "BF_LISTING_VERIFICATION_REQUIRED": {"status": 409, "severity": "warning", "parameters": []},

  "BF_MOBILE_SYNC_CONFLICT": {"status": 409, "severity": "warning", "parameters": []},
  "BF_PERSON_PROFILE_INVALID": {"status": 400, "severity": "error", "parameters": []},
  "BF_PERSON_ACCOUNT_INVALID": {"status": 400, "severity": "error", "parameters": []},
  "BF_PERSON_MESSAGE_INVALID": {"status": 400, "severity": "error", "parameters": []},
  "BF_PERSON_MESSAGE_CONFLICT": {"status": 409, "severity": "warning", "parameters": []},
  "BF_PERSON_CODE_UNAVAILABLE": {"status": 409, "severity": "warning", "parameters": []},
  "BF_WEBSITE_MEDIA_INVALID": {"status": 400, "severity": "error", "parameters": []},
  "BF_WEBSITE_MEDIA_IN_USE": {"status": 409, "severity": "warning", "parameters": []},
  "BF_WEBSITE_SITE_KEY_INVALID": {"status": 400, "severity": "error", "parameters": []},
  "BF_WEBSITE_DETAILS_INVALID": {"status": 400, "severity": "error", "parameters": []},
  "BF_WEBSITE_THEME_INVALID": {"status": 400, "severity": "error", "parameters": []},
  "BF_WEBSITE_PAGE_INVALID": {"status": 400, "severity": "error", "parameters": []},
  "BF_WEBSITE_SECTION_INVALID": {"status": 400, "severity": "error", "parameters": []},
  "BF_WEBSITE_STATE": {"status": 409, "severity": "warning", "parameters": []},
  "BF_WEBSITE_SITE_EXISTS": {"status": 409, "severity": "warning", "parameters": []},
  "BF_WEBSITE_SITE_KEY_TAKEN": {"status": 409, "severity": "warning", "parameters": []},
  "BF_WEBSITE_PAGE_SLUG_TAKEN": {"status": 409, "severity": "warning", "parameters": []},
  "BF_WEBSITE_PAGE_REQUIRED": {"status": 409, "severity": "warning", "parameters": []},
  "BF_WEBSITE_PUBLISH_INCOMPLETE": {"status": 409, "severity": "warning", "parameters": []},
  "BF_WEBSITE_REQUEST_CONFLICT": {"status": 409, "severity": "warning", "parameters": []},
  "BF_CONSENT_INVALID": {"status": 400, "severity": "error", "parameters": []},
  "BF_CONSENT_TEXT_INVALID": {"status": 400, "severity": "error", "parameters": []},
  "BF_CONSENT_LINK_INVALID": {"status": 400, "severity": "error", "parameters": []},
  "BF_CONSENT_TYPE_CONFLICT": {"status": 409, "severity": "warning", "parameters": []},
  "BF_CONSENT_SYSTEM_TYPE": {"status": 409, "severity": "warning", "parameters": []},
  "BF_CONSENT_TYPE_IN_USE": {"status": 409, "severity": "warning", "parameters": []},
  "BF_CONSENT_OPERATION_CONFLICT": {"status": 409, "severity": "warning", "parameters": []},
  "BF_CONSENT_TYPE_DISABLED": {"status": 409, "severity": "warning", "parameters": []},
  "BF_CONSENT_REQUIRED": {"status": 403, "severity": "warning", "parameters": []},
  "BF_CONSENT_TEXT_OUTDATED": {"status": 409, "severity": "warning", "parameters": []},
  "BF_CONSENT_NOT_WITHDRAWABLE": {"status": 409, "severity": "warning", "parameters": []},
  "BF_SMS_INVALID": {"status": 400, "severity": "error", "parameters": []},
  "BF_SMS_PHONE_INVALID": {"status": 400, "severity": "error", "parameters": []},
  "BF_SMS_TEMPLATE_PARAMETER_INVALID": {"status": 400, "severity": "error", "parameters": []},
  "BF_SMS_ACCOUNT_REQUIRED": {"status": 409, "severity": "warning", "parameters": []},
  "BF_SMS_CREDIT_INSUFFICIENT": {"status": 409, "severity": "warning", "parameters": []},
  "BF_SMS_TEMPLATE_CONFLICT": {"status": 409, "severity": "warning", "parameters": []},
  "BF_SMS_PHONE_NOT_VERIFIED": {"status": 409, "severity": "warning", "parameters": []},
  "BF_USER_INVALID": {"status": 400, "severity": "error", "parameters": []},
  "BF_USER_PASSWORD_WEAK": {"status": 400, "severity": "error", "parameters": []},
  "BF_USER_EXISTS": {"status": 409, "severity": "warning", "parameters": []},
  "BF_USER_ROLE_FORBIDDEN": {"status": 400, "severity": "error", "parameters": []},
  "BF_USER_SELF_CHANGE": {"status": 400, "severity": "error", "parameters": []},
  "BF_USER_LINK_CONFLICT": {"status": 409, "severity": "warning", "parameters": []},
  "BF_IDENTITY_UNAVAILABLE": {"status": 503, "severity": "error", "parameters": []},
  "BF_MOBILE_REGISTRATION_INVALID": {"status": 400, "severity": "error", "parameters": []},
  "BF_MOBILE_REGISTRATION_CONFLICT": {"status": 409, "severity": "warning", "parameters": []},
  "BF_MOBILE_RATE_LIMITED": {"status": 409, "severity": "warning", "parameters": []},
  "BF_MOBILE_LINK_REJECTED": {"status": 409, "severity": "warning", "parameters": []},
  "BF_MOBILE_LINK_LOCKED": {"status": 409, "severity": "warning", "parameters": []},

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
  "BF_SURVEY_INVALID": {"status": 400, "severity": "error", "parameters": []},
  "BF_SURVEY_QUESTION_INVALID": {"status": 400, "severity": "error", "parameters": []},
  "BF_SURVEY_ANSWER_INVALID": {"status": 400, "severity": "error", "parameters": []},
  "BF_SURVEY_STATE": {"status": 409, "severity": "warning", "parameters": []},
  "BF_SURVEY_ALREADY_ANSWERED": {"status": 409, "severity": "warning", "parameters": []},
  "BF_SURVEY_NOT_OPEN": {"status": 409, "severity": "warning", "parameters": []},
  "BF_PERSON_DUPLICATE_IDENTITY": {"status": 409, "severity": "warning", "parameters": []},
  "BF_PERSON_DUPLICATE_PHONE": {"status": 409, "severity": "warning", "parameters": []},
  "BF_PERSON_DUPLICATE_EMAIL": {"status": 409, "severity": "warning", "parameters": []},
  "BF_PERSON_LINK_UNAVAILABLE": {"status": 409, "severity": "warning", "parameters": []},
  "BF_LEASE_OVERLAP": {"status": 409, "severity": "warning", "parameters": []},
  "BF_LEASE_PARTY_OUTSIDE": {"status": 409, "severity": "warning", "parameters": []},
  "BF_PLAN_INVALID": {"status": 400, "severity": "error", "parameters": []},
  "BF_PARKING_INVALID": {"status": 400, "severity": "error", "parameters": []},
  "BF_PARKING_POLICY_REQUIRED": {"status": 409, "severity": "warning", "parameters": []},
  "BF_PARKING_QUOTA_EXCEEDED": {"status": 409, "severity": "warning", "parameters": []},
  "BF_PARKING_PLATE_TAKEN": {"status": 409, "severity": "warning", "parameters": []},
  "BF_PARKING_SPACE_OCCUPIED": {"status": 409, "severity": "warning", "parameters": []},
  "BF_PARKING_IN_USE": {"status": 409, "severity": "warning", "parameters": []},
  "BF_PARKING_STATE_INVALID": {"status": 409, "severity": "warning", "parameters": []},
  "BF_PARKING_RELATION_REQUIRED": {"status": 409, "severity": "warning", "parameters": []},
  "BF_PARKING_REQUEST_REUSED": {"status": 409, "severity": "warning", "parameters": []},
  "BF_PARKING_NAME_TAKEN": {"status": 409, "severity": "warning", "parameters": []},
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
