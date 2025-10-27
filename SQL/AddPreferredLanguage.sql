-- Add PreferredLanguage column to AspNetUsers table
ALTER TABLE AspNetUsers
ADD PreferredLanguage NVARCHAR(10) NOT NULL DEFAULT 'en';
