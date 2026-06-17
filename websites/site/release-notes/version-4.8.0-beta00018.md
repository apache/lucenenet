---
uid: releasenotes/4.8.0-beta00018
version: 4.8.0-beta00018
---

# Lucene.NET 4.8.0-beta00018 Release Notes

---

> This is a maintenance update that upgrades ICU4N to the latest version, since several serious concurrency and resource loading bugs have been patched since the last Lucene.NET release.

<!-- Release notes generated using configuration in .github/release.yml at Lucene.Net_4_8_0_beta00018 -->

## What's Changed
### 🐞 Bug Fixes
* FuzzyQuery produces a wrong result when prefix is equal to the term length by @paulirwin in https://github.com/apache/lucenenet/pull/1002
### 🚀 Performance Improvements
* SWEEP: Replace J2N's TripleShift call with C# 11's unsigned right shift operator by @paulirwin in https://github.com/apache/lucenenet/pull/1007
### 🏆 Improvements
* Added "Improvements" Category for Release Notes by @NightOwl888 in https://github.com/apache/lucenenet/pull/1015
### 📄 Website and API Documentation
* website/site/.htaccess - bug fix by removing BOM and update to beta0017 redirection by @rclabo in https://github.com/apache/lucenenet/pull/1005
* Updated .htaccess copy and release procedure by @NightOwl888 in https://github.com/apache/lucenenet/pull/1010
* Added GitHub Automation for Release Notes by @NightOwl888 in https://github.com/apache/lucenenet/pull/1011
### 💪 Other Changes
* ShingleFilter produces invalid queries by @tohidemyname in https://github.com/apache/lucenenet/pull/946
* Fix SegmentInfos replace doesn't update userData by @tohidemyname in https://github.com/apache/lucenenet/pull/948

## New Contributors
* @tohidemyname made their first contribution in https://github.com/apache/lucenenet/pull/946

**Full Changelog**: https://github.com/apache/lucenenet/compare/Lucene.Net_4_8_0_beta00017...Lucene.Net_4_8_0_beta00018
