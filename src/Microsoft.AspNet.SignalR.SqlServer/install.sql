-- Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

DECLARE @SCHEMA_NAME nvarchar(32),
		@SCHEMA_TABLE_NAME nvarchar(100),
		@TARGET_SCHEMA_VERSION int,
		@MESSAGE_TABLE_NAME nvarchar(100),
		@MESSAGE_TABLE_COUNT int,
        @CREATE_MESSAGE_TABLE_DDL nvarchar(1000),
		@CREATE_MESSAGE_ID_TABLE_DDL nvarchar(1000);

SET @SCHEMA_NAME = 'SignalR'; -- replaced from C#
SET @SCHEMA_TABLE_NAME = 'Schema'; -- replaced from C#
SET @TARGET_SCHEMA_VERSION = 1; -- replaced from C#
SET @MESSAGE_TABLE_COUNT = 3; -- replaced from C#
SET @MESSAGE_TABLE_NAME = 'Messages'; -- replaced from C#
SET @CREATE_MESSAGE_TABLE_DDL =
N'CREATE TABLE [' + @SCHEMA_NAME + N'].[@TableName](
    [PayloadId] [bigint] NOT NULL,
	[Payload] [varbinary](max) NOT NULL,
	[InsertedOn] [datetime] NOT NULL,
	PRIMARY KEY CLUSTERED ([PayloadId] ASC)
);'
SET @CREATE_MESSAGE_ID_TABLE_DDL =
N'CREATE TABLE [' + @SCHEMA_NAME + N'].[@TableName] (
    [PayloadId] [bigint] NOT NULL,
	PRIMARY KEY CLUSTERED ([PayloadId] ASC)
);';

/*CREATE TRIGGER [' + @SCHEMA_NAME + N'].[@TableName_Trim]
   ON [' + @SCHEMA_NAME + N'].[@TableName]
   AFTER INSERT
AS
BEGIN
	DECLARE @MaxTableSize int,
			@BlockSize int,
			@MaxPayloadId bigint;

	SET @MaxTableSize = 10000;
	SET @BlockSize = 2500;

	-- Get max ID from inserted row
	SELECT @MaxPayloadId = [PayloadId]
	FROM Inserted;

	-- Check the table size on every Nth insert where N is @BlockSize
	IF @MaxPayloadId % @BlockSize = 0
		BEGIN
			-- SET NOCOUNT ON added to prevent extra result sets from
			-- interfering with SELECT statements
			SET NOCOUNT ON;

			DECLARE @RowCount int,
					@StartPayloadId bigint,
					@EndPayloadId bigint;

			SELECT @RowCount = COUNT([PayloadId]), @StartPayloadId = MIN([PayloadId])
			FROM [' + @SCHEMA_NAME + N'].[@TableName];

			-- Check if we''re over the max table size
			IF @RowCount >= @MaxTableSize
				BEGIN
					DECLARE @OverMaxBy int;

					-- We want to delete enough rows to bring the table back to max size - block size
					SET @OverMaxBy = @RowCount - @MaxTableSize;
					SET @EndPayloadId = @StartPayloadId + @BlockSize + @OverMaxBy;
 
					-- Delete oldest block of messages
					DELETE FROM [' + @SCHEMA_NAME + N'].[@TableName]
					WHERE [PayloadId] BETWEEN @StartPayloadId AND @EndPayloadId;
			    END
	    END
END';*/

PRINT 'Installing SignalR SQL objects';

SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;
BEGIN TRANSACTION;

-- Create the DB schema if it doesn't exist
IF NOT EXISTS(SELECT [schema_id] FROM [sys].[schemas] WHERE [name] = @SCHEMA_NAME)
	BEGIN
		BEGIN TRY
			EXEC(N'CREATE SCHEMA [' + @SCHEMA_NAME + '];');
			PRINT 'Created database schema [' + @SCHEMA_NAME  + ']';
		END TRY
		BEGIN CATCH
            DECLARE @ErrorNumber int,
                    @ErrorSeverity int,
                    @ErrorState int;

            SELECT @ErrorNumber = ERROR_NUMBER(),
                   @ErrorSeverity = ERROR_SEVERITY(),
                   @ErrorState = ERROR_STATE();

			IF @ErrorNumber = 2759
				-- If it's an object already exists error then ignore
				PRINT 'Database schema [' + @SCHEMA_NAME + '] already exists'
			ELSE
				RAISERROR (@ErrorNumber, @ErrorSeverity, @ErrorState);
		END CATCH;
	END
ELSE
	PRINT 'Database schema [' + @SCHEMA_NAME  + '] already exists';

DECLARE @SCHEMA_ID int;
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

-- Install tables, etc.
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
						DECLARE @ddl nvarchar(max);
						
						-- Create the message table
						SET @table_name = @MESSAGE_TABLE_NAME + '_' + CONVERT(nvarchar, @counter);
						SET @ddl = REPLACE(@CREATE_MESSAGE_TABLE_DDL, '@TableName', @table_name);
						
						IF NOT EXISTS(SELECT [object_id]
									  FROM [sys].[tables]
									  WHERE [name] = @table_name
									    AND [schema_id] = @SCHEMA_ID)
							BEGIN
								EXEC(@ddl);
								PRINT 'Created message table [' + @SCHEMA_NAME + '].[' + @table_name + ']';
							END
						ELSE
							PRINT 'Mesage table [' + @SCHEMA_NAME + '].[' + @table_name + '] already exists';

						-- Create the id table
						SET @table_name = @table_name + '_Id';
						SET @ddl = REPLACE(@CREATE_MESSAGE_ID_TABLE_DDL, '@TableName', @table_name);

						IF NOT EXISTS(SELECT [object_id]
									  FROM [sys].[tables]
									  WHERE [name] = @table_name
										AND [schema_id] = @SCHEMA_ID)
							BEGIN
								EXEC(@ddl);
								PRINT 'Created message ID table [' + @SCHEMA_NAME + '].[PayloadId]';
							END
						ELSE
							PRINT 'Message ID table [' + @SCHEMA_NAME + '].[' + @table_name + '] alread exists';

						SET @counter = @counter + 1;
					END
				
				IF @CURRENT_SCHEMA_VERSION IS NULL
					BEGIN
						DECLARE @insert_dml nvarchar(1000);
						SET @CURRENT_SCHEMA_VERSION = 1;
						SET @insert_dml = 'INSERT INTO [' + @SCHEMA_NAME  + '].[' + @SCHEMA_TABLE_NAME + '] ([SchemaVersion]) VALUES(' + CONVERT(nvarchar, @CURRENT_SCHEMA_VERSION) + ')';
						EXEC(@insert_dml);
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
		RAISERROR(N'SignalR database current schema version %d is newer than the configured SqlMessageBus schema version %d. Please update to the latest Microsoft.AspNet.SignalR.SqlServer NuGet package.', 11, 1,
			@CURRENT_SCHEMA_VERSION, @TARGET_SCHEMA_VERSION);
	END