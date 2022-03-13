import json
from xml.dom.minidom import AttributeList
import requests
import pandas as pd

# Load JSON
url = 'https://raw.githubusercontent.com/seekingtau/stargaze-rarity/main/metadata_scrape/stargaze_asset_attribute_dump_console/stargaze_asset_attribute_dump_console/results-Stargaze%20Punks.json'
jsonData = requests.get(url,headers={'Host':'raw.githubusercontent.com','Content-Type':'application/json'})

jsonData = json.loads(jsonData.text)

attributeList = []
traitValList = []

for item in jsonData:
    itemAttributes = item['attributes']
    attributeList.append(itemAttributes)
    
for x in attributeList:
    for y in x:
        traitValue = {y['trait_type'], y['value']}
        traitValList.append(traitValue)
        
counts = pd.Series(traitValList).value_counts()
counts.to_csv('counts.csv')