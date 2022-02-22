# RoutingCLI

Command line interface for finding routes between one or more coordinate pairs.

Warning: must be run with custom binary encoded road network file (get it from Erlend).

Run RoutingCLI.exe "C:\path\to\config.json".

Example config:

    {
	    "searches": [
            {
                "source": [
                    242541.20553505284,
                    7021740.248120374
                ],
                "target": [
                    253416.78153408598,
                    7131740.248120374
                ]
            },
            {
                "source": [
                    253416.78153408598,
                    7030691.380750257
                ],
                "target": [
                    242541.20553505284,
                    7030621.380750257
                ]
            },
            {
                "source": [
                    267243.5700083785,
                    6764480.177584507
                ],
                "target": [
                    262640.4187730937,
                    6711480.177584507
                ]
            }
        ],
	    "networkPath": "vegnettRuteplan_FGDB_20210528_tolerance-0001.bin",
	    "resultsPath": "results.json"
    }
