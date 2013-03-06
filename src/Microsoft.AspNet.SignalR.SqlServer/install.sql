DECLARE @SCHEMA_NAME nvarchar,
		@SCHEMA_ID int,
		@SCHEMA_TABLE_NAME nvarchar,
		@TARGET_SCHEMA_VERSION int,
		@CURRENT_SCHEMA_VERSION int;

SET @SCHEMA_NAME = 'SignalR';
SET @SCHEMA_TABLE_NAME = 'SignalR_Schema';
SET @TARGET_SCHEMA_VERSION = 1;

-- Create the DB schema if it doesn't exist
IF NOT EXISTS(SELECT [schema_id] FROM [sys].[schemas] WHERE [name] = @SCHEMA_NAME)
	BEGIN
		EXEC('CREATE SCHEMA [' + @SCHEMA_NAME + ']');
		SELECT @SCHEMA_ID = [schema_id] FROM [sys].[schemas] WHERE [name] = @SCHEMA_NAME;
	END

-- Create the SignalR_Schema table if it doesn't exist
IF NOT EXISTS(SELECT [object_id] FROM [sys].[tables] WHERE [name] = @SCHEMA_TABLE_NAME)
	BEGIN
		-- No SignalR schema, just install everything

		CREATE TABLE [SignalR].[SignalR_Schema](
			[SchemaVersion] [int] NOT NULL,
			PRIMARY KEY CLUSTERED ([SchemaVersion] ASC)
		);

		CREATE TABLE [SignalR].[SignalR_Messages](
			[PayloadId] [bigint] IDENTITY(1,1) NOT NULL,
			[Payload] [varbinary](max) NOT NULL,
			[InsertedOn] [datetime] NOT NULL,
		PRIMARY KEY CLUSTERED ([PayloadId] ASC)
		);
	END
ELSE
	BEGIN
		-- Check the schema version
		SELECT @CURRENT_SCHEMA_VERSION = [SchemaVersion] FROM [SignalR].[SignalR_Schema];
		IF @CURRENT_SCHEMA_VERSION IS NULL OR @CURRENT_SCHEMA_VERSION < @TARGET_SCHEMA_VERSION
			BEGIN
				-- Update to new schema version

			END
		ELSE IF @CURRENT_SCHEMA_VERSION = @TARGET_SCHEMA_VERSION
			BEGIN
				-- Schema up to date, let's make sure we have all the message tables as configured
				-- TODO: Ensure all message tables are created
				SELECT GETDATE() -- dummy
			END
		ELSE IF @CURRENT_SCHEMA_VERSION > @TARGET_SCHEMA_VERSION
			THROW 50001, ''
	END
