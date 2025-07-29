-- Add OtpVerification table manually
CREATE TABLE IF NOT EXISTS "OtpVerifications" (
    "Id" text NOT NULL,
    "PhoneNumber" character varying(20) NOT NULL,
    "OtpCode" character varying(6) NOT NULL,
    "CreatedAt" timestamp without time zone NOT NULL,
    "ExpiresAt" timestamp without time zone NOT NULL,
    "IsUsed" boolean NOT NULL DEFAULT false,
    "UsedAt" timestamp without time zone,
    "AttemptCount" integer NOT NULL DEFAULT 0,
    CONSTRAINT "PK_OtpVerifications" PRIMARY KEY ("Id")
);

-- Add indexes for performance
CREATE INDEX IF NOT EXISTS "IX_OtpVerifications_ExpiresAt" ON "OtpVerifications" ("ExpiresAt");
CREATE INDEX IF NOT EXISTS "IX_OtpVerifications_PhoneNumber_CreatedAt" ON "OtpVerifications" ("PhoneNumber", "CreatedAt");