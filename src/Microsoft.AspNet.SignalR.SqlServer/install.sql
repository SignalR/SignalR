DECLARE @SCHEMA_NAME nvarchar(32),
		@SCHEMA_ID int,
		@SCHEMA_TABLE_NAME nvarchar(100),
		@TARGET_SCHEMA_VERSION int,
		@CREATE_MESSAGE_TABLE_DML nvarchar(1000),
		@MESSAGE_TABLE_NAME nvarchar(100),
		@MESSAGE_TABLE_COUNT int;

SET @SCHEMA_NAME = 'SignalR'; -- replaced from C#
SET @SCHEMA_TABLE_NAME = 'Schema'; -- replaced from C#
SET @TARGET_SCHEMA_VERSION = 1; -- replaced from C#
SET @MESSAGE_TABLE_COUNT = 3; -- replaced from C#
SET @MESSAGE_TABLE_NAME = 'Messages'; -- replaced from C#
SET @CREATE_MESSAGE_TABLE_DML = N'CREATE TABLE [' + @SCHEMA_NAME + N'].[@TableName](' +
					N'[PayloadId] [bigint] IDENTITY(1,1) NOT NULL,' +
					N'[Payload] [varbinary](max) NOT NULL,' +
					N'[InsertedOn] [datetime] NOT NULL,' +
					N'PRIMARY KEY CLUSTERED ([PayloadId] ASC)' +
				N');';

PRINT 'Installing SignalR SQL objects';

SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;
BEGIN TRANSACTION;

-- Create the DB schema if it doesn't exist
IF NOT EXISTS(SELECT [schema_id] FROM [sys].[schemas] WHERE [name] = @SCHEMA_NAME)
	BEGIN
		EXEC(N'CREATE SCHEMA [' + @SCHEMA_NAME + '];');		
		PRINT 'Created database schema [' + @SCHEMA_NAME  + ']';
	END
ELSE
	PRINT 'Database schema [' + @SCHEMA_NAME  + '] already exists';

SELECT @SCHEMA_ID = [schema_id] FROM [sys].[schemas] WHERE [name] = @SCHEMA_NAME;

-- Create the SignalR_Schema table if it doesn't exist
IF NOT EXISTS(SELECT [object_id] FROM [sys].[tables] WHERE [name] = @SCHEMA_TABLE_NAME AND [schema_id] = @SCHEMA_ID)
	BEGIN
		-- Create the empty SignalR schema table
		EXEC(N'CREATE TABLE [' + @SCHEMA_NAME  + '].[' + @SCHEMA_TABLE_NAME + ']('+
			 N'[SchemaVersion] [int] NOT NULL,'+
			 N'PRIMARY KEY CLUSTERED ([SchemaVersion] ASC)'+
		     N')');
		PRINT 'Created table [' + @SCHEMA_NAME  + '].[' + @SCHEMA_TABLE_NAME + ']';
	END
ELSE
	PRINT 'Table [' + @SCHEMA_NAME  + '].[' + @SCHEMA_TABLE_NAME + '] already exists';

DECLARE @GET_SCHEMA_VERSION_SQL nvarchar(1000);
SET @GET_SCHEMA_VERSION_SQL = N'SELECT @schemaVersion = [SchemaVersion] FROM [' + @SCHEMA_NAME  + N'].[' + @SCHEMA_TABLE_NAME + N']';

DECLARE @CURRENT_SCHEMA_VERSION int;
EXEC sp_executesql @GET_SCHEMA_VERSION_SQL, N'@schemaVersion int output',
	@schemaVersion = @CURRENT_SCHEMA_VERSION output;

PRINT 'Current SignalR schema version: ' + CASE @CURRENT_SCHEMA_VERSION WHEN NULL THEN 'none' ELSE CONVERT(nvarchar, @CURRENT_SCHEMA_VERSION) END;

IF @CURRENT_SCHEMA_VERSION IS NULL OR @CURRENT_SCHEMA_VERSION <= @TARGET_SCHEMA_VERSION
	BEGIN
		IF @CURRENT_SCHEMA_VERSION IS NULL OR @CURRENT_SCHEMA_VERSION = @TARGET_SCHEMA_VERSION
			BEGIN
				-- Install version 1
				PRINT 'Installing schema version 1';
				DECLARE @counter int;
				SET @counter = 1;
				WHILE @counter <= @MESSAGE_TABLE_COUNT
					BEGIN
						DECLARE @table_name nvarchar(100);
						DECLARE @dml nvarchar(max);
						SET @table_name = @MESSAGE_TABLE_NAME + '_' + CONVERT(nvarchar, @counter);
						SET @dml = REPLACE(@CREATE_MESSAGE_TABLE_DML, '@TableName', @table_name);
						
						IF NOT EXISTS(SELECT [object_id]
									  FROM [sys].[tables]
									  WHERE [name] = @table_name
									    AND [schema_id] = @SCHEMA_ID)
							BEGIN
								EXEC(@dml);
								PRINT 'Created message table [' + @SCHEMA_NAME + '].[' + @table_name + ']';
							END
						ELSE
							PRINT 'Mesage table [' + @SCHEMA_NAME + '].[' + @table_name + '] already exists';

						SET @counter = @counter + 1;
					END
				
				IF @CURRENT_SCHEMA_VERSION IS NULL
					BEGIN
						SET @CURRENT_SCHEMA_VERSION = 1;
						EXEC('INSERT INTO [' + @SCHEMA_NAME  + '].[' + @SCHEMA_TABLE_NAME + '] ([SchemaVersion]) VALUES(@CURRENT_SCHEMA_VERSION)');
					END
				
				PRINT 'Schema version 1 installed';
			END
		/*IF @CURRENT_SCHEMA_VERSION = 1
			BEGIN
				-- Update to version 2
				-- TODO: Add schema updates here when we go to v2
				SELECT GETDATE() -- dummy
			END*/

		COMMIT TRANSACTION;

		PRINT 'SignalR SQL objects installed';
	END

ELSE -- @CURRENT_SCHEMA_VERSION > @TARGET_SCHEMA_VERSION
	BEGIN
		-- Configured SqlMessageBus is lower version than current DB schema, just bail out
		ROLLBACK TRANSACTION;
		RAISERROR(N'SignalR database current schema version %a is newer than the configured SqlMessageBus schema version %b. Please update to the latest Microsoft.AspNet.SignalR.SqlServer package.', 11, 1,
			@CURRENT_SCHEMA_VERSION, @TARGET_SCHEMA_VERSION);
	END