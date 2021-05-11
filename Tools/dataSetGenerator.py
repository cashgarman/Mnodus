import csv
import random
import uuid

# Settings
maxSitesPerCompany = 5
minPeoplePerSite = 5
maxPeoplePerSite = 25
numCompanies = 20
minNumberEmails = 5
maxNumberEmails = 25

maleFirstNames = open("maleFirstNames.txt").readlines()
femaleFirstNames = open("femaleFirstNames.txt").readlines()
lastNames = open("lastNames.txt").readlines()
companyNames = open("companyNames.txt").readlines()
countryNames = open("countryNames.txt").readlines()
industryNames = open("industryNames.txt").readlines()
emailSubjects = open("emailSubjects.txt").readlines()

def GetID(rows, name):
	for i, row in enumerate(rows):
		if row["name"] is name:
			return i

def GetRandomID(rows):
	return random.randint(0, len(rows) - 1)

def GeneratePersonName(male):
	if male:
		return "%s %s" % (random.choice(maleFirstNames).strip(), random.choice(lastNames).strip())
	else:
		return "%s %s" % (random.choice(femaleFirstNames).strip(), random.choice(lastNames).strip())

# Nodes
countries = []
for i in range(0, len(countryNames)):
	countries.append({"id": str(uuid.uuid4()), "name": countryNames[i].strip().replace(',', '')})

companies = []
for i in range(0, len(companyNames)):
	companies.append({"id": str(uuid.uuid4()), "name": companyNames[i].strip().replace(',', '')})

industries = []
for i in range(0, len(industryNames)):
	industries.append({"id": str(uuid.uuid4()), "name": industryNames[i].strip()})

sites = []
company_site = []
company_industry = []
site_country = []
for company in companies:
	numSites = random.randint(0, maxSitesPerCompany)
	company_industry.append({"source": company["id"], "target": industries[random.randint(0, len(industries) - 1)]["id"]})
	for i in range(0, numSites):
		siteID = str(uuid.uuid4())
		sites.append({"id": siteID, "name": "%s Site %d" % (company["name"].strip().replace(',','') , i + 1)})
		company_site.append({"source": company["id"], "target": siteID})
		site_country.append({"source": siteID, "target": countries[random.randint(0, len(countries) - 1)]["id"]})

people = []
person_site = []
for site in sites:
	numPeople = random.randint(minPeoplePerSite, maxPeoplePerSite)
	for i in range(0, numPeople):
		personID = str(uuid.uuid4())
		people.append({"id": personID, "name": GeneratePersonName(random.choice([True, False]))})
		person_site.append({"source": personID, "target": site["id"]})

def GetSourcesWithTarget(relationstips, sourceData, target):
	found = []
	for relationship in relationstips:
		if relationship["target"] is target["id"]:
			found.append([t for t in sourceData if t["id"] == relationship["source"]][0])
	return found

# def GetTargetsWithSource(sourceData, targetData, source):
# 	found = []
# 	for targetDatum in targetData:
# 		if targetDatum["target"] is target["id"]:
# 			found.append([t for t in targetData if t["id"] == target["id"]])
# 	return found

emails = []
email_person = []
for site in sites:
	workers = GetSourcesWithTarget(person_site, people, site)
	for worker in workers:
		numEmails = random.randint(minNumberEmails, maxNumberEmails)
		for j in range(0, numEmails):
			emailID = str(uuid.uuid4())
			emailSubject = "%s%s" % (random.choice(["","RE: "]), random.choice(emailSubjects).strip())
			sender = random.choice(workers)
			senderID = sender["id"]
			recipientID = worker["id"]
			emails.append(
				{
					"id": emailID,
					"name": emailSubject,
					"important": True if random.random() < 0.1 else False
				})
			email_person.append({"type": "sender", "source": emailID, "target": senderID})
			email_person.append({"type": "recipient", "source": emailID, "target": recipientID})

def writeCSV(rows, outputPath, addIDs = True):
	with open(outputPath, 'wb') as csvfile:
		if addIDs:
			writer = csv.DictWriter(csvfile, rows[0].keys())
			writer.writeheader()
			for row in rows:
				writer.writerow(row)
		else:
			writer = csv.DictWriter(csvfile, rows[0].keys())
			writer.writeheader()
			writer.writerows(rows)

writeCSV(countries, "../Assets/Resources/Datasets/countries.csv")
writeCSV(industries, "../Assets/Resources/Datasets/industries.csv")
writeCSV(companies, "../Assets/Resources/Datasets/companies.csv")
writeCSV(sites, "../Assets/Resources/Datasets/sites.csv")
writeCSV(people, "../Assets/Resources/Datasets/people.csv")
writeCSV(emails, "../Assets/Resources/Datasets/emails.csv")

writeCSV(company_industry, "../Assets/Resources/Datasets/company_industry.csv", False)
writeCSV(company_site, "../Assets/Resources/Datasets/company_site.csv", False)
writeCSV(site_country, "../Assets/Resources/Datasets/site_country.csv", False)
writeCSV(person_site, "../Assets/Resources/Datasets/person_site.csv", False)
writeCSV(email_person, "../Assets/Resources/Datasets/email_person.csv", False)

print "Generated %d countries" % len(countries)
print "Generated %d industries" % len(industries)
print "Generated %d companies" % len(companies)
print "Generated %d sites" % len(sites)
print "Generated %d people" % len(people)
print "Generated %d emails" % len(emails)