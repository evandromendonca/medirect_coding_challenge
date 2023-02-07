-- create database medirect_currency_exchange

-- DROP TABLE public.currency_rate;
CREATE TABLE public.currency_rate (
	id int4 NOT NULL GENERATED ALWAYS AS IDENTITY,
	date_created timestamptz NOT NULL,
	client_id int4 NOT NULL,
	rate_provider varchar(25) NULL,
	base_currency varchar(3) NOT NULL,
	target_currency varchar(3) NOT NULL,
	value numeric NOT NULL,
	rate_timestamp timestamptz NOT NULL,
	CONSTRAINT currency_rate_pk PRIMARY KEY (id)
);
CREATE INDEX currency_rate_client_id_idx ON public.currency_rate USING btree (client_id, base_currency, target_currency);

-- DROP TABLE public.currency_trade;
CREATE TABLE public.currency_trade (
	id int4 NOT NULL GENERATED ALWAYS AS IDENTITY,
	date_created timestamptz NOT NULL,
	client_id int4 NOT NULL,
	base_currency varchar(3) NOT NULL,
	target_currency varchar(3) NOT NULL,
	rate numeric NOT NULL,
	base_currency_amount numeric NOT NULL,
	fees numeric NOT NULL DEFAULT 0,
	target_currency_amount numeric NOT NULL,
	rate_id int4 NOT NULL,
	CONSTRAINT currency_trade_pk PRIMARY KEY (id)
);
CREATE INDEX currency_trade_client_id_idx ON public.currency_trade USING btree (client_id, date_created);