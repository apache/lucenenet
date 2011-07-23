package view;

=head1 INTERFACE

Each function within view.pm which will be used for page generation must
implement a standard interface.

    sub my_view {
        my %args = @_;
        ...
        return ($content, $extension, @optional);
    }

First, each function must accept labeled parameters.  The only parameter which
will always be present is "path"; see the documentation in path.pm for the
"@patterns" array with regards to invocation with additional parameters.

Second, each function must return a list with at least two elements: the first
element must be the page content, and the second must be a file extention.
Returning additional elements in the list (as some of the functions below do)
is optional.

    return ($content, 'html', \%args);

The constraints imposed by this interface may cause difficulties, for example
when you want to generate both "foo.html" and "foo.pdf".  However, it is
usually possible to work around such issues with symlinks and dependency
management in path.pm.

=cut

use strict;
use warnings;
use Carp;
use Switch;
use Dotiac::DTL;
use Dotiac::DTL::Addon::markup;
use ASF::Util qw( read_text_file );
use File::Basename;

BEGIN { push @Dotiac::DTL::TEMPLATE_DIRS, "templates"; }

# A "basic" view, which takes 'template' and 'path' parameters.

sub basic {
    my %args = @_;
    my $filepath = "content$args{path}";
    read_text_file($filepath, \%args);
    $args{path} =~ s/\.mdtext$/\.html/;
    $args{breadcrumbs} = _breadcrumbs($args{path});
	my $template_path = "templates/$args{template}";
    my $rendered = Dotiac::DTL->new($template_path)->render(\%args);
    return ($rendered, 'html', \%args);
}

# A view which generates a sitemap.

sub sitemap {
    my %args = @_;
    my $template = "content$args{path}";
    $args{breadcrumbs} .= _breadcrumbs($args{path});
    my $dir = $template;
    $dir =~ s!/[^/]+$!!;
    opendir my $dh, $dir or die "Can't opendir $dir: $!\n";
    my %data;
    for (map "$dir/$_", grep $_ ne "." && $_ ne ".." && $_ ne ".svn", readdir $dh) {
        if (-f and /\.mdtext$/) {
            my $file = $_;
            $file =~ s/^content//;
            no warnings 'once';
            for my $p (@path::patterns) {
                my ($re, $method, $args) = @$p;
                next unless $file =~ $re;
                my $s = view->can($method) or die "Can't locate method: $method\n";
                my ($content, $ext, $vars) = $s->(path => $file, %$args);
                $file =~ s/\.mdtext$/.$ext/;
                $data{$file} = $vars;
                last;
            }
        }
    }

    my $content = "";

    for (sort keys %data) {
        $content .= "- [$data{$_}->{headers}->{title}]($_)\n";
        for my $hdr (grep /^#/, split "\n", $data{$_}->{content}) {
            $hdr =~ /^(#+)\s+([^#]+)?\s+\1\s+\{#([^}]+)\}$/ or next;
            my $level = length $1;
            $level *= 4;
            $content .= " " x $level;
            $content .= "- [$2]($_#$3)\n";
        }
    }
    $args{content} = $content;
    return Dotiac::DTL::Template($template)->render(\%args), html => \%args;
}




sub _breadcrumbs {
    my @path = split m!/!, shift;
    pop @path;
    my @rv;
    my $relpath = "";
    for (@path) {
        $relpath .= "$_/";
        $_ ||= "Home";
        push @rv, qq(<a href="$relpath">\u$_</a>);
    }
    return join "&nbsp;&raquo&nbsp;", @rv;
}

1;

__END__

=head1 LICENSE

    Licensed to the Apache Software Foundation (ASF) under one or more
    contributor license agreements.  See the NOTICE file distributed with
    this work for additional information regarding copyright ownership.  The
    ASF licenses this file to you under the Apache License, Version 2.0 (the
    "License"); you may not use this file except in compliance with the
    License.  You may obtain a copy of the License at
    
        http://www.apache.org/licenses/LICENSE-2.0
    
    Unless required by applicable law or agreed to in writing, software
    distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
    WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.  See the
    License for the specific language governing permissions and limitations
    under the License.

=cut

